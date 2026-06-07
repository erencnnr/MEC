using System.Globalization;
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
        private static readonly Regex DayHeaderRegex = new(
            @"^(?:(?<weekday>pazartesi|sal[ıi]|çarşamba|carsamba|perşembe|persembe|cuma|cumartesi|pazar)\s*(?<day>\d{1,2})(?:[./-](?<month>\d{1,2}))?|(?<day>\d{1,2})(?:[./-](?<month>\d{1,2}))?\s*(?<weekday>pazartesi|sal[ıi]|çarşamba|carsamba|perşembe|persembe|cuma|cumartesi|pazar))(?<rest>.*)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly string[] NoiseTokens =
        {
            "yemek menüsü",
            "yemek menusu",
            "bumed",
            "mec",
            "okulları",
            "okullari",
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
            "ocak",
            "şubat",
            "subat",
            "mart",
            "nisan",
            "mayıs",
            "mayis",
            "koşuyolu",
            "kosuyolu",
            "arnavutköy",
            "arnavutkoy",
            "bahçeköy",
            "bahcekoy",
            "genel müdürlük",
            "genel mudurluk"
        };

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

            var savedDays = await _foodMenuDayRepository.GetAllAsync(x => x.FoodMenuMonthId == month.Id);
            return OperationResultModel<FoodMenuMonthModel>.Success(
                MapFoodMenuMonth(month, savedDays),
                parseResult.Days.Count == 0
                    ? "PDF yüklendi. Günlük menüler otomatik çıkarılamadı; lütfen manuel düzenleyin."
                    : "PDF yüklendi ve günlük menüler taslak olarak hazırlandı.",
                parseResult.Days.Count == 0 ? "warning" : "success");
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
                result.Warnings.Add("PDF içeriği okunamadı.");
                return result;
            }

            try
            {
                using var stream = new MemoryStream(pdfContent);
                using var document = PdfDocument.Open(stream);
                result.PageCount = document.NumberOfPages;

                var builders = new Dictionary<DateTime, FoodMenuDayBuilder>();

                foreach (var page in document.GetPages())
                {
                    var lines = SplitLines(page.Text);
                    FoodMenuDayBuilder? currentDay = null;

                    foreach (var rawLine in lines)
                    {
                        var line = NormalizeLine(rawLine);
                        if (string.IsNullOrWhiteSpace(line) || IsNoiseLine(line))
                        {
                            continue;
                        }

                        if (TryParseDayHeader(line, year, month, out var parsedDate, out var trailingText))
                        {
                            if (!builders.TryGetValue(parsedDate, out currentDay))
                            {
                                currentDay = new FoodMenuDayBuilder
                                {
                                    MenuDate = parsedDate,
                                    SourcePageNumber = page.Number
                                };
                                builders[parsedDate] = currentDay;
                            }

                            if (!string.IsNullOrWhiteSpace(trailingText) && !IsNoiseLine(trailingText))
                            {
                                currentDay.Lines.Add(trailingText);
                            }

                            continue;
                        }

                        if (currentDay == null || !LooksLikeMenuContent(line))
                        {
                            continue;
                        }

                        currentDay.Lines.Add(line);
                    }
                }

                var orderedBuilders = builders.Values
                    .OrderBy(x => x.MenuDate)
                    .ToList();

                for (var index = 0; index < orderedBuilders.Count; index++)
                {
                    var builder = orderedBuilders[index];
                    result.Days.Add(new FoodMenuDayModel
                    {
                        MenuDate = builder.MenuDate,
                        ItemsText = NormalizeItemsText(string.Join(Environment.NewLine, builder.Lines)),
                        SourcePageNumber = builder.SourcePageNumber,
                        DisplayOrder = index + 1
                    });
                }

                if (result.Days.Count == 0)
                {
                    result.Warnings.Add("PDF içinden gün başlıkları çıkarılamadı.");
                }
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"PDF parse işlemi tamamlanamadı: {ex.Message}");
            }

            return result;
        }

        private static IEnumerable<string> SplitLines(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<string>();
            }

            return text
                .Replace("\r", "\n")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        private static bool TryParseDayHeader(string line, int year, int fallbackMonth, out DateTime menuDate, out string trailingText)
        {
            menuDate = default;
            trailingText = string.Empty;

            var normalizedLine = NormalizeForMatch(line);
            var match = DayHeaderRegex.Match(normalizedLine);
            if (!match.Success)
            {
                return false;
            }

            if (!int.TryParse(match.Groups["day"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dayNumber))
            {
                return false;
            }

            var monthNumber = fallbackMonth;
            if (int.TryParse(match.Groups["month"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedMonth))
            {
                monthNumber = parsedMonth;
            }

            if (monthNumber < 1 || monthNumber > 12)
            {
                return false;
            }

            if (dayNumber < 1 || dayNumber > DateTime.DaysInMonth(year, monthNumber))
            {
                return false;
            }

            menuDate = new DateTime(year, monthNumber, dayNumber);
            var rawTrailingText = line.Length > match.Length
                ? line[match.Length..]
                : string.Empty;
            trailingText = NormalizeLine(rawTrailingText.Trim(' ', '-', ':', ';', '.', '|'));
            return true;
        }

        private static bool LooksLikeMenuContent(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            if (line.Length < 2)
            {
                return false;
            }

            if (Regex.IsMatch(line, @"^\d+$"))
            {
                return false;
            }

            return !IsNoiseLine(line);
        }

        private static bool IsNoiseLine(string line)
        {
            var normalized = NormalizeForMatch(line);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return true;
            }

            return NoiseTokens.Any(token => normalized.Contains(token, StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeItemsText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var lines = text
                .Replace("\r", "\n")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(NormalizeLine)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            return string.Join(Environment.NewLine, lines);
        }

        private static string NormalizeLine(string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return string.Empty;
            }

            return Regex.Replace(line.Replace('\u00A0', ' '), @"\s+", " ").Trim();
        }

        private static string NormalizeForMatch(string text)
        {
            return NormalizeLine(text)
                .ToLowerInvariant()
                .Replace("ş", "s")
                .Replace("ı", "i")
                .Replace("ğ", "g")
                .Replace("ü", "u")
                .Replace("ö", "o")
                .Replace("ç", "c");
        }

        private sealed class FoodMenuParseResult
        {
            public int PageCount { get; set; }
            public List<string> Warnings { get; } = new();
            public List<FoodMenuDayModel> Days { get; } = new();
        }

        private sealed class FoodMenuDayBuilder
        {
            public DateTime MenuDate { get; set; }
            public int SourcePageNumber { get; set; }
            public List<string> Lines { get; } = new();
        }
    }
}
