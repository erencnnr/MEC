using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MEC.Application.Abstractions.Common.Models;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Application.Abstractions.Service.SchoolService.Model;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Common.Enum;
using MEC.Domain.Entity.School;
using UglyToad.PdfPig;

namespace MEC.Application.Service.SchoolService
{
    public class FoodMenuService : IFoodMenuService
    {
        private static readonly Regex DateRegex = new(
            @"(?<day>\d{1,2})[./-](?<month>\d{1,2})[./-](?<year>\d{4})",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly string[] NoiseTokens =
        {
            "yemek menüsü",
            "yemek menusu",
            "bumed",
            "bumed mec okulları",
            "bumed mec okullari",
            "okulları",
            "okullari",
            "ocak",
            "şubat",
            "subat",
            "mart",
            "nisan",
            "mayıs",
            "mayis",
            "haziran",
            "temmuz",
            "ağustos",
            "agustos",
            "eylül",
            "eylul",
            "ekim",
            "kasım",
            "kasim",
            "aralık",
            "aralik",
            "koşuyolu",
            "kosuyolu",
            "arnavutköy",
            "arnavutkoy",
            "bahçeköy",
            "bahcekoy",
            "genel müdürlük",
            "genel mudurluk",
            "aylık yemek planı",
            "sağlıklı ve afiyetli günler",
            "saglikli ve afiyetli gunler",
            "afiyet olsun",
            "menüde değişiklik olabilir",
            "menude degisiklik olabilir"
        };

        private static readonly string[] WeekdayTokens =
        {
            "pazartesi",
            "salı",
            "sali",
            "çarşamba",
            "carsamba",
            "perşembe",
            "persembe",
            "cuma",
            "cumartesi",
            "pazar"
        };

        private const double DateHeaderRowTolerance = 8d;
        private const double DateHeaderToContentGap = 6d;
        private const double CellLineTolerance = 3.5d;
        private const double NextWeekHeaderGap = 8d;
        private const double DayNameSearchWidth = 120d;

        private readonly IGenericRepository<FoodMenuMonth> _foodMenuMonthRepository;
        private readonly IGenericRepository<FoodMenuDay> _foodMenuDayRepository;

        public FoodMenuService(
            IGenericRepository<FoodMenuMonth> foodMenuMonthRepository,
            IGenericRepository<FoodMenuDay> foodMenuDayRepository)
        {
            _foodMenuMonthRepository = foodMenuMonthRepository;
            _foodMenuDayRepository = foodMenuDayRepository;
        }

        public async Task<List<FoodMenuMonthModel>> GetFoodMenuMonthsAsync()
        {
            var months = (await _foodMenuMonthRepository.GetAllAsync())
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ThenByDescending(x => x.Id)
                .ToList();

            var days = (await _foodMenuDayRepository.GetAllAsync())
                .OrderBy(x => x.MenuDate)
                .ThenBy(x => x.DisplayOrder)
                .ToList();

            return months
                .Select(x => MapFoodMenuMonth(x, days.Where(day => day.FoodMenuMonthId == x.Id)))
                .ToList();
        }

        public async Task<FoodMenuMonthModel?> GetFoodMenuMonthAsync(int id)
        {
            var month = await _foodMenuMonthRepository.GetByIdAsync(id);
            if (month == null)
            {
                return null;
            }

            var days = await _foodMenuDayRepository.GetAllAsync(x => x.FoodMenuMonthId == id);
            return MapFoodMenuMonth(month, days);
        }

        public async Task<FoodMenuMonthModel?> GetFoodMenuMonthByYearMonthAsync(int year, int month)
        {
            var existing = (await _foodMenuMonthRepository.GetAllAsync(x => x.Year == year && x.Month == month))
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();

            if (existing == null)
            {
                return null;
            }

            var days = await _foodMenuDayRepository.GetAllAsync(x => x.FoodMenuMonthId == existing.Id);
            return MapFoodMenuMonth(existing, days);
        }

        public async Task<OperationResultModel<FoodMenuMonthModel>> EnsureDraftFoodMenuMonthAsync(int year, int month)
        {
            if (month < 1 || month > 12)
            {
                return OperationResultModel<FoodMenuMonthModel>.Fail("Geçerli bir ay seçiniz.");
            }

            var existing = (await _foodMenuMonthRepository.GetAllAsync(x => x.Year == year && x.Month == month))
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();

            if (existing != null)
            {
                var existingDays = await _foodMenuDayRepository.GetAllAsync(x => x.FoodMenuMonthId == existing.Id);
                return OperationResultModel<FoodMenuMonthModel>.Success(MapFoodMenuMonth(existing, existingDays));
            }

            var entity = new FoodMenuMonth
            {
                Year = year,
                Month = month,
                Status = FoodMenuMonthStatus.Draft,
                CreatedDate = DateTime.Now,
                UpdateDate = DateTime.Now
            };

            await _foodMenuMonthRepository.AddAsync(entity);
            return OperationResultModel<FoodMenuMonthModel>.Success(MapFoodMenuMonth(entity, Array.Empty<FoodMenuDay>()));
        }

        public async Task<OperationResultModel<FoodMenuMonthModel>> ReplaceImportedFoodMenuAsync(FoodMenuImportModel model)
        {
            var month = await _foodMenuMonthRepository.GetByIdAsync(model.MonthId);
            if (month == null)
            {
                return OperationResultModel<FoodMenuMonthModel>.Fail("Yemek menüsü ay kaydı bulunamadı.");
            }

            var parseResult = ParseFoodMenu(model.PdfContent, model.Year, model.Month);

            var existingDays = (await _foodMenuDayRepository.GetAllAsync(x => x.FoodMenuMonthId == month.Id)).ToList();
            foreach (var day in existingDays)
            {
                _foodMenuDayRepository.Delete(day);
            }

            month.Year = model.Year;
            month.Month = model.Month;
            month.FileName = model.FileName;
            month.OriginalFileName = model.OriginalFileName;
            month.RelativePath = model.RelativePath;
            month.ContentType = model.ContentType;
            month.SizeBytes = model.SizeBytes;
            month.PageCount = parseResult.PageCount;
            month.ParseWarnings = string.Join(Environment.NewLine, parseResult.Warnings);
            month.Status = FoodMenuMonthStatus.Draft;
            month.ImportedAt = DateTime.Now;
            month.PublishedAt = null;
            month.UpdateDate = DateTime.Now;
            _foodMenuMonthRepository.Update(month);

            foreach (var parsedDay in parseResult.Days.OrderBy(x => x.MenuDate).ThenBy(x => x.DisplayOrder))
            {
                await _foodMenuDayRepository.AddAsync(new FoodMenuDay
                {
                    FoodMenuMonthId = month.Id,
                    MenuDate = parsedDay.MenuDate,
                    ItemsText = parsedDay.ItemsText,
                    SourcePageNumber = parsedDay.SourcePageNumber,
                    DisplayOrder = parsedDay.DisplayOrder,
                    CreatedDate = DateTime.Now,
                    UpdateDate = DateTime.Now
                });
            }

            var parsedContentDayCount = parseResult.Days.Count(x => !string.IsNullOrWhiteSpace(x.ItemsText));
            var savedDays = await _foodMenuDayRepository.GetAllAsync(x => x.FoodMenuMonthId == month.Id);
            return OperationResultModel<FoodMenuMonthModel>.Success(
                MapFoodMenuMonth(month, savedDays),
                parsedContentDayCount == 0
                    ? "PDF yüklendi. Günlük menüler otomatik çıkarılamadı; lütfen manuel düzenleyin."
                    : "PDF yüklendi ve günlük menüler taslak olarak hazırlandı.",
                parsedContentDayCount == 0 ? "warning" : "success");
        }

        public async Task<OperationResultModel> SaveFoodMenuDaysAsync(FoodMenuMonthDaySaveModel model)
        {
            var month = await _foodMenuMonthRepository.GetByIdAsync(model.MonthId);
            if (month == null)
            {
                return OperationResultModel.Fail("Yemek menüsü ay kaydı bulunamadı.");
            }

            var existingDays = (await _foodMenuDayRepository.GetAllAsync(x => x.FoodMenuMonthId == month.Id)).ToList();
            foreach (var day in existingDays)
            {
                _foodMenuDayRepository.Delete(day);
            }

            var normalizedDays = model.Days
                .Where(x => !string.IsNullOrWhiteSpace(x.ItemsText) || x.SourcePageNumber.HasValue)
                .OrderBy(x => x.MenuDate)
                .ToList();

            for (var index = 0; index < normalizedDays.Count; index++)
            {
                var day = normalizedDays[index];
                await _foodMenuDayRepository.AddAsync(new FoodMenuDay
                {
                    FoodMenuMonthId = month.Id,
                    MenuDate = day.MenuDate.Date,
                    ItemsText = NormalizeItemsText(day.ItemsText),
                    SourcePageNumber = day.SourcePageNumber.HasValue && day.SourcePageNumber.Value > 0
                        ? day.SourcePageNumber
                        : null,
                    DisplayOrder = index + 1,
                    CreatedDate = DateTime.Now,
                    UpdateDate = DateTime.Now
                });
            }

            month.Status = FoodMenuMonthStatus.Draft;
            month.UpdateDate = DateTime.Now;
            _foodMenuMonthRepository.Update(month);

            return OperationResultModel.Success("Yemek menüsü günleri kaydedildi.");
        }

        public async Task<OperationResultModel> PublishFoodMenuMonthAsync(int id)
        {
            var month = await _foodMenuMonthRepository.GetByIdAsync(id);
            if (month == null)
            {
                return OperationResultModel.Fail("Yayınlanacak yemek menüsü bulunamadı.");
            }

            var days = (await _foodMenuDayRepository.GetAllAsync(x => x.FoodMenuMonthId == id))
                .Where(x => !string.IsNullOrWhiteSpace(x.ItemsText))
                .ToList();

            if (days.Count == 0)
            {
                return OperationResultModel.Fail("Yayınlama için en az bir gün menüsü doldurulmalıdır.");
            }

            month.Status = FoodMenuMonthStatus.Published;
            month.PublishedAt = DateTime.Now;
            month.UpdateDate = DateTime.Now;
            _foodMenuMonthRepository.Update(month);

            return OperationResultModel.Success("Yemek menüsü yayınlandı.");
        }

        public async Task<OperationResultModel> DeleteFoodMenuMonthAsync(int id)
        {
            var month = await _foodMenuMonthRepository.GetByIdAsync(id);
            if (month == null)
            {
                return OperationResultModel.Fail("Silinecek yemek menüsü bulunamadı.");
            }

            var days = (await _foodMenuDayRepository.GetAllAsync(x => x.FoodMenuMonthId == id)).ToList();
            foreach (var day in days)
            {
                _foodMenuDayRepository.Delete(day);
            }

            _foodMenuMonthRepository.Delete(month);
            return OperationResultModel.Success("Yemek menüsü silindi.");
        }

        public async Task<List<FoodMenuMonthModel>> GetPublishedFoodMenuMonthsAsync()
        {
            var months = (await _foodMenuMonthRepository.GetAllAsync(x => x.Status == FoodMenuMonthStatus.Published))
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ThenByDescending(x => x.PublishedAt)
                .ToList();

            var days = (await _foodMenuDayRepository.GetAllAsync())
                .OrderBy(x => x.MenuDate)
                .ThenBy(x => x.DisplayOrder)
                .ToList();

            return months
                .Select(x => MapFoodMenuMonth(x, days.Where(day => day.FoodMenuMonthId == x.Id)))
                .ToList();
        }

        public async Task<FoodMenuMonthModel?> GetLatestPublishedFoodMenuMonthAsync()
        {
            var month = (await _foodMenuMonthRepository.GetAllAsync(x => x.Status == FoodMenuMonthStatus.Published))
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ThenByDescending(x => x.PublishedAt)
                .FirstOrDefault();

            if (month == null)
            {
                return null;
            }

            var days = await _foodMenuDayRepository.GetAllAsync(x => x.FoodMenuMonthId == month.Id);
            return MapFoodMenuMonth(month, days);
        }

        public async Task<FoodMenuMonthModel?> GetPublishedFoodMenuMonthAsync(int id)
        {
            var month = await _foodMenuMonthRepository.GetByIdAsync(id);
            if (month == null || month.Status != FoodMenuMonthStatus.Published)
            {
                return null;
            }

            var days = await _foodMenuDayRepository.GetAllAsync(x => x.FoodMenuMonthId == id);
            return MapFoodMenuMonth(month, days);
        }

        private static FoodMenuMonthModel MapFoodMenuMonth(FoodMenuMonth month, IEnumerable<FoodMenuDay> days)
        {
            return new FoodMenuMonthModel
            {
                Id = month.Id,
                Year = month.Year,
                Month = month.Month,
                Status = month.Status,
                FileName = month.FileName,
                OriginalFileName = month.OriginalFileName,
                RelativePath = month.RelativePath,
                ContentType = month.ContentType,
                SizeBytes = month.SizeBytes,
                PageCount = month.PageCount,
                ParseWarnings = month.ParseWarnings,
                ImportedAt = month.ImportedAt,
                PublishedAt = month.PublishedAt,
                CreatedDate = month.CreatedDate,
                Days = days
                    .OrderBy(x => x.MenuDate)
                    .ThenBy(x => x.DisplayOrder)
                    .Select(MapFoodMenuDay)
                    .ToList()
            };
        }

        private static FoodMenuDayModel MapFoodMenuDay(FoodMenuDay day)
        {
            return new FoodMenuDayModel
            {
                Id = day.Id,
                MenuDate = day.MenuDate,
                ItemsText = day.ItemsText,
                SourcePageNumber = day.SourcePageNumber,
                DisplayOrder = day.DisplayOrder
            };
        }

        private static FoodMenuParseResult ParseFoodMenu(byte[] pdfContent, int year, int month)
        {
            var result = new FoodMenuParseResult();

            if (pdfContent.Length == 0)
            {
                AddWarning(result.Warnings, "PDF içeriği okunamadı.");
                return result;
            }

            try
            {
                using var stream = new MemoryStream(pdfContent);
                using var document = PdfDocument.Open(stream);
                result.PageCount = document.NumberOfPages;

                var parsedDays = new Dictionary<DateTime, ParsedMenuDay>();

                foreach (var page in document.GetPages())
                {
                    var words = page.GetWords()
                        .Select(word => new PdfWord(
                            NormalizeLine(word.Text),
                            word.BoundingBox.Left,
                            word.BoundingBox.Right,
                            word.BoundingBox.Bottom,
                            word.BoundingBox.Top))
                        .Where(word => !string.IsNullOrWhiteSpace(word.Text))
                        .ToList();

                    if (words.Count == 0)
                    {
                        continue;
                    }

                    var pageResult = ParsePage(words, year, month, page.Number);
                    foreach (var warning in pageResult.Warnings)
                    {
                        AddWarning(result.Warnings, warning);
                    }

                    foreach (var day in pageResult.Days)
                    {
                        if (!parsedDays.TryGetValue(day.MenuDate.Date, out var existingDay))
                        {
                            parsedDays[day.MenuDate.Date] = day;
                            continue;
                        }

                        parsedDays[day.MenuDate.Date] = ChoosePreferredDay(existingDay, day);
                    }
                }

                var orderedDays = parsedDays.Values
                    .OrderBy(x => x.MenuDate)
                    .ToList();

                for (var index = 0; index < orderedDays.Count; index++)
                {
                    var day = orderedDays[index];
                    result.Days.Add(new FoodMenuDayModel
                    {
                        MenuDate = day.MenuDate,
                        ItemsText = day.ItemsText,
                        SourcePageNumber = day.SourcePageNumber,
                        DisplayOrder = index + 1
                    });
                }

                if (result.Days.Count == 0)
                {
                    AddWarning(result.Warnings, "PDF içinden gün başlıkları çıkarılamadı.");
                }
            }
            catch (Exception ex)
            {
                AddWarning(result.Warnings, $"PDF parse işlemi tamamlanamadı: {ex.Message}");
            }

            return result;
        }

        private static FoodMenuPageParseResult ParsePage(List<PdfWord> words, int year, int month, int pageNumber)
        {
            var result = new FoodMenuPageParseResult();
            var headers = FindDateHeaders(words, year, month);
            if (headers.Count == 0)
            {
                return result;
            }

            var pageLeft = words.Min(x => x.Left);
            var pageRight = words.Max(x => x.Right);
            var pageBottom = words.Min(x => x.Bottom) - 2d;

            var weekRows = GroupRows(headers, x => x.CenterY, DateHeaderRowTolerance)
                .Select(row => row.OrderBy(x => x.CenterX).ToList())
                .OrderByDescending(row => row.Average(x => x.CenterY))
                .ToList();

            for (var weekIndex = 0; weekIndex < weekRows.Count; weekIndex++)
            {
                var week = weekRows[weekIndex];
                if (week.Count == 0)
                {
                    continue;
                }

                var currentRowY = week.Average(x => x.CenterY);
                var nextRowY = weekIndex < weekRows.Count - 1
                    ? weekRows[weekIndex + 1].Average(x => x.CenterY)
                    : pageBottom;
                var estimatedColumnWidth = EstimateColumnWidth(week, pageRight - pageLeft);

                for (var dayIndex = 0; dayIndex < week.Count; dayIndex++)
                {
                    var header = week[dayIndex];
                    var leftBoundary = dayIndex == 0
                        ? Math.Max(pageLeft, header.CenterX - (estimatedColumnWidth / 2d))
                        : (week[dayIndex - 1].CenterX + header.CenterX) / 2d;
                    var rightBoundary = dayIndex == week.Count - 1
                        ? Math.Min(pageRight, header.CenterX + (estimatedColumnWidth / 2d))
                        : (header.CenterX + week[dayIndex + 1].CenterX) / 2d;

                    var cellWords = words
                        .Where(word =>
                            word.CenterX >= leftBoundary &&
                            word.CenterX < rightBoundary &&
                            word.CenterY < currentRowY - DateHeaderToContentGap &&
                            word.CenterY > nextRowY + NextWeekHeaderGap)
                        .ToList();

                    var lines = BuildLines(cellWords, CellLineTolerance);
                    var sections = ParseSections(lines);
                    var itemsText = BuildItemsText(lines, sections);

                    if (lines.Count == 0)
                    {
                        AddWarning(result.Warnings, $"{header.Date:dd.MM.yyyy} günü bulundu ancak hücre içeriği çıkarılamadı.");
                    }
                    else if (!sections.HasAnySections)
                    {
                        AddWarning(result.Warnings, $"{header.Date:dd.MM.yyyy} günü bulundu ancak bölüm başlıkları çıkarılamadı.");
                    }
                    else if (sections.SectionCount < 4)
                    {
                        AddWarning(result.Warnings, $"{header.Date:dd.MM.yyyy} günü bulundu ancak bazı bölüm başlıkları eksik kaldı.");
                    }

                    result.Days.Add(new ParsedMenuDay
                    {
                        MenuDate = header.Date,
                        DayName = header.DayName,
                        ItemsText = itemsText,
                        SourcePageNumber = pageNumber
                    });
                }
            }

            return result;
        }

        private static List<DateHeader> FindDateHeaders(List<PdfWord> words, int year, int month)
        {
            var headers = new List<DateHeader>();

            foreach (var word in words)
            {
                var normalizedWord = NormalizeForMatch(word.Text);
                var match = DateRegex.Match(normalizedWord);
                if (!match.Success)
                {
                    continue;
                }

                if (!TryBuildDate(match, year, month, out var menuDate))
                {
                    continue;
                }

                var trailingText = NormalizeLine(normalizedWord[(match.Index + match.Length)..]);
                var dayName = IsWeekdayLine(trailingText)
                    ? NormalizeLine(trailingText)
                    : string.Join(" ", words
                        .Where(other =>
                            !ReferenceEquals(other, word) &&
                            Math.Abs(other.CenterY - word.CenterY) <= 4d &&
                            other.Left >= word.Right - 2d &&
                            other.Left <= word.Right + DayNameSearchWidth &&
                            IsWeekdayLine(other.Text))
                        .OrderBy(other => other.Left)
                        .Select(other => NormalizeLine(other.Text)));

                headers.Add(new DateHeader
                {
                    Date = menuDate,
                    DayName = dayName,
                    CenterX = word.CenterX,
                    CenterY = word.CenterY
                });
            }

            return headers
                .GroupBy(x => x.Date.Date)
                .Select(group => group
                    .OrderByDescending(x => x.CenterY)
                    .ThenBy(x => x.CenterX)
                    .First())
                .OrderByDescending(x => x.CenterY)
                .ThenBy(x => x.CenterX)
                .ToList();
        }

        private static bool TryBuildDate(Match match, int year, int expectedMonth, out DateTime menuDate)
        {
            menuDate = default;

            if (!int.TryParse(match.Groups["day"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var day))
            {
                return false;
            }

            if (!int.TryParse(match.Groups["month"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var month))
            {
                return false;
            }

            if (!int.TryParse(match.Groups["year"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedYear))
            {
                return false;
            }

            if (parsedYear != year || month != expectedMonth)
            {
                return false;
            }

            if (day < 1 || day > DateTime.DaysInMonth(year, month))
            {
                return false;
            }

            menuDate = new DateTime(year, month, day);
            return true;
        }

        private static double EstimateColumnWidth(IReadOnlyList<DateHeader> headers, double pageWidth)
        {
            if (headers.Count <= 1)
            {
                return Math.Max(pageWidth, 120d);
            }

            var gaps = headers
                .Zip(headers.Skip(1), (left, right) => right.CenterX - left.CenterX)
                .Where(gap => gap > 20d)
                .ToList();

            if (gaps.Count == 0)
            {
                return Math.Max(pageWidth / headers.Count, 120d);
            }

            return Math.Max(gaps.Average(), 80d);
        }

        private static List<string> BuildLines(List<PdfWord> words, double tolerance)
        {
            if (words.Count == 0)
            {
                return new List<string>();
            }

            return GroupRows(words, word => word.CenterY, tolerance)
                .Select(row => NormalizeLine(string.Join(" ", row
                    .OrderBy(word => word.Left)
                    .Select(word => word.Text))))
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();
        }

        private static MenuSections ParseSections(List<string> lines)
        {
            var sections = new MenuSections();
            MenuSectionType? currentSection = null;

            foreach (var rawLine in lines)
            {
                var line = NormalizeLine(rawLine);
                if (string.IsNullOrWhiteSpace(line) || IsNoiseLine(line))
                {
                    continue;
                }

                var sectionType = TryGetSectionType(line);
                if (sectionType.HasValue)
                {
                    sections.FoundSections.Add(sectionType.Value);
                    currentSection = sectionType.Value;
                    continue;
                }

                if (currentSection.HasValue)
                {
                    sections.GetLines(currentSection.Value).Add(line);
                }
                else
                {
                    sections.Unassigned.Add(line);
                }
            }

            return sections;
        }

        private static MenuSectionType? TryGetSectionType(string line)
        {
            var normalized = NormalizeForMatch(line).Trim(' ', ':', ';', '-', '|');
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return null;
            }

            if (normalized.StartsWith("kahvalti", StringComparison.Ordinal))
            {
                return MenuSectionType.Breakfast;
            }

            if (normalized.StartsWith("ogle", StringComparison.Ordinal) && normalized.Contains("yemeg", StringComparison.Ordinal))
            {
                return MenuSectionType.Lunch;
            }

            if (normalized.StartsWith("salata bar", StringComparison.Ordinal))
            {
                return MenuSectionType.SaladBar;
            }

            if (normalized.StartsWith("ikindi", StringComparison.Ordinal))
            {
                return MenuSectionType.Afternoon;
            }

            return null;
        }

        private static string BuildItemsText(List<string> lines, MenuSections sections)
        {
            if (lines.Count == 0)
            {
                return string.Empty;
            }

            if (!sections.HasAnySections)
            {
                return NormalizeItemsText(string.Join(Environment.NewLine, lines));
            }

            var blocks = new List<string>();
            AppendSectionBlock(blocks, "Kahvaltı", sections.Breakfast);
            AppendSectionBlock(blocks, "Öğle Yemeği", sections.Lunch);
            AppendSectionBlock(blocks, "Salata Bar", sections.SaladBar);
            AppendSectionBlock(blocks, "İkindi", sections.Afternoon);

            if (sections.Unassigned.Count > 0)
            {
                blocks.Add(string.Join(Environment.NewLine, sections.Unassigned));
            }

            return NormalizeItemsText(string.Join(
                $"{Environment.NewLine}{Environment.NewLine}",
                blocks.Where(block => !string.IsNullOrWhiteSpace(block))));
        }

        private static void AppendSectionBlock(List<string> blocks, string title, IReadOnlyCollection<string> items)
        {
            if (items.Count == 0)
            {
                return;
            }

            blocks.Add(string.Join(Environment.NewLine, new[] { title }.Concat(items)));
        }

        private static List<List<T>> GroupRows<T>(IEnumerable<T> items, Func<T, double> getY, double tolerance)
        {
            var rows = new List<List<T>>();

            foreach (var item in items.OrderByDescending(getY))
            {
                var y = getY(item);
                var row = rows.FirstOrDefault(existingRow => Math.Abs(existingRow.Average(getY) - y) <= tolerance);
                if (row == null)
                {
                    rows.Add(new List<T> { item });
                }
                else
                {
                    row.Add(item);
                }
            }

            return rows
                .OrderByDescending(row => row.Average(getY))
                .ToList();
        }

        private static ParsedMenuDay ChoosePreferredDay(ParsedMenuDay existingDay, ParsedMenuDay candidateDay)
        {
            var existingScore = ScoreDay(existingDay);
            var candidateScore = ScoreDay(candidateDay);
            return candidateScore > existingScore ? candidateDay : existingDay;
        }

        private static int ScoreDay(ParsedMenuDay day)
        {
            var contentLength = string.IsNullOrWhiteSpace(day.ItemsText) ? 0 : day.ItemsText.Length;
            return (contentLength * 10) + (string.IsNullOrWhiteSpace(day.DayName) ? 0 : 1);
        }

        private static bool IsNoiseLine(string line)
        {
            var normalized = NormalizeForMatch(line);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return true;
            }

            if (DateRegex.IsMatch(normalized))
            {
                return true;
            }

            if (IsWeekdayLine(normalized))
            {
                return true;
            }

            if (Regex.IsMatch(normalized, @"^[\W_]+$"))
            {
                return true;
            }

            return NoiseTokens.Any(token => normalized.Contains(NormalizeForMatch(token), StringComparison.Ordinal));
        }

        private static bool IsWeekdayLine(string? line)
        {
            var normalized = NormalizeForMatch(line);
            return WeekdayTokens.Any(token => normalized.Equals(NormalizeForMatch(token), StringComparison.Ordinal));
        }

        private static string NormalizeItemsText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var rawLines = text
                .Replace("\r", "\n")
                .Split('\n');

            var normalizedLines = new List<string>();
            var previousBlank = true;

            foreach (var rawLine in rawLines)
            {
                var line = NormalizeLine(rawLine);
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (!previousBlank && normalizedLines.Count > 0)
                    {
                        normalizedLines.Add(string.Empty);
                        previousBlank = true;
                    }

                    continue;
                }

                normalizedLines.Add(line);
                previousBlank = false;
            }

            while (normalizedLines.Count > 0 && string.IsNullOrWhiteSpace(normalizedLines[^1]))
            {
                normalizedLines.RemoveAt(normalizedLines.Count - 1);
            }

            return string.Join(Environment.NewLine, normalizedLines);
        }

        private static string NormalizeLine(string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return string.Empty;
            }

            var sanitized = FixCommonMojibake(line)
                .Replace('\u00A0', ' ')
                .Replace('\u00AD', '-')
                .Replace('\uFFFE', '-');

            sanitized = Regex.Replace(sanitized, @"\s+", " ");
            sanitized = Regex.Replace(sanitized, @"\s*-\s*", "-");
            return sanitized.Trim();
        }

        private static string NormalizeForMatch(string? text)
        {
            var normalized = NormalizeLine(text);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return string.Empty;
            }

            normalized = FixCommonMojibake(normalized).Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                var lower = char.ToLowerInvariant(character);
                builder.Append(lower switch
                {
                    'ı' => 'i',
                    'İ' => 'i',
                    _ => lower
                });
            }

            return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
        }

        private static string FixCommonMojibake(string text)
        {
            return text
                .Replace("â€™", "'")
                .Replace("â€“", "-")
                .Replace("â€”", "-")
                .Replace("Ã§", "ç")
                .Replace("Ã‡", "Ç")
                .Replace("ÄŸ", "ğ")
                .Replace("Äž", "Ğ")
                .Replace("Ä±", "ı")
                .Replace("Ä°", "İ")
                .Replace("Ã¶", "ö")
                .Replace("Ã–", "Ö")
                .Replace("Ã¼", "ü")
                .Replace("Ãœ", "Ü")
                .Replace("ÅŸ", "ş")
                .Replace("Åž", "Ş");
        }

        private static void AddWarning(List<string> warnings, string warning)
        {
            if (string.IsNullOrWhiteSpace(warning))
            {
                return;
            }

            if (warnings.Any(existing => string.Equals(existing, warning, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            warnings.Add(warning);
        }

        private sealed class FoodMenuParseResult
        {
            public int PageCount { get; set; }
            public List<string> Warnings { get; } = new();
            public List<FoodMenuDayModel> Days { get; } = new();
        }

        private sealed class FoodMenuPageParseResult
        {
            public List<string> Warnings { get; } = new();
            public List<ParsedMenuDay> Days { get; } = new();
        }

        private sealed class ParsedMenuDay
        {
            public DateTime MenuDate { get; set; }
            public string DayName { get; set; } = string.Empty;
            public string ItemsText { get; set; } = string.Empty;
            public int SourcePageNumber { get; set; }
        }

        private sealed class MenuSections
        {
            public HashSet<MenuSectionType> FoundSections { get; } = new();
            public List<string> Breakfast { get; } = new();
            public List<string> Lunch { get; } = new();
            public List<string> SaladBar { get; } = new();
            public List<string> Afternoon { get; } = new();
            public List<string> Unassigned { get; } = new();

            public bool HasAnySections => FoundSections.Count > 0;
            public int SectionCount => FoundSections.Count;

            public List<string> GetLines(MenuSectionType sectionType)
            {
                return sectionType switch
                {
                    MenuSectionType.Breakfast => Breakfast,
                    MenuSectionType.Lunch => Lunch,
                    MenuSectionType.SaladBar => SaladBar,
                    MenuSectionType.Afternoon => Afternoon,
                    _ => Unassigned
                };
            }
        }

        private sealed class PdfWord
        {
            public PdfWord(string text, double left, double right, double bottom, double top)
            {
                Text = text;
                Left = left;
                Right = right;
                Bottom = bottom;
                Top = top;
            }

            public string Text { get; }
            public double Left { get; }
            public double Right { get; }
            public double Bottom { get; }
            public double Top { get; }
            public double CenterX => (Left + Right) / 2d;
            public double CenterY => (Bottom + Top) / 2d;
        }

        private sealed class DateHeader
        {
            public DateTime Date { get; set; }
            public string DayName { get; set; } = string.Empty;
            public double CenterX { get; set; }
            public double CenterY { get; set; }
        }

        private enum MenuSectionType
        {
            Breakfast = 1,
            Lunch = 2,
            SaladBar = 3,
            Afternoon = 4
        }
    }
}
