using System.Globalization;
using System.Text;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Portal.Models;
using MEC.Portal.Services;
using Microsoft.AspNetCore.Mvc;

namespace MEC.Portal.Controllers
{
    public class FoodListController : Controller
    {
        private readonly IFoodMenuService _foodMenuService;
        private readonly IFoodMenuAttachmentApiClient _foodMenuAttachmentApiClient;

        public FoodListController(
            IFoodMenuService foodMenuService,
            IFoodMenuAttachmentApiClient foodMenuAttachmentApiClient)
        {
            _foodMenuService = foodMenuService;
            _foodMenuAttachmentApiClient = foodMenuAttachmentApiClient;
        }

        [HttpGet("/FoodList")]
        public async Task<IActionResult> Index(int? monthId = null, string? date = null)
        {
            var publishedMonths = await _foodMenuService.GetPublishedFoodMenuMonthsAsync();
            if (publishedMonths.Count == 0)
            {
                return View(new FoodListViewModel());
            }

            var selectedMonth = monthId.HasValue
                ? publishedMonths.FirstOrDefault(x => x.Id == monthId.Value)
                : publishedMonths.FirstOrDefault();

            selectedMonth ??= publishedMonths.FirstOrDefault();
            if (selectedMonth == null)
            {
                return View(new FoodListViewModel());
            }

            var selectedDay = ResolveSelectedDay(selectedMonth, date);
            return View(new FoodListViewModel
            {
                Months = publishedMonths.Select(x => new FoodListMonthOptionViewModel
                {
                    Id = x.Id,
                    Label = BuildMonthLabel(x.Year, x.Month)
                }).ToList(),
                SelectedMonthId = selectedMonth.Id,
                SelectedMonthLabel = BuildMonthLabel(selectedMonth.Year, selectedMonth.Month),
                PdfPreviewUrl = string.IsNullOrWhiteSpace(selectedMonth.FileName)
                    ? string.Empty
                    : Url.Action(nameof(Pdf), "FoodList", new { id = selectedMonth.Id }) ?? string.Empty,
                PdfFileName = selectedMonth.OriginalFileName,
                Days = selectedMonth.Days
                    .OrderBy(x => x.MenuDate)
                    .Select(x => new FoodListDayOptionViewModel
                    {
                        QueryValue = x.MenuDate.ToString("yyyy-MM-dd"),
                        Label = x.MenuDate.ToString("dd MMMM dddd", new CultureInfo("tr-TR")),
                        IsSelected = selectedDay != null && x.MenuDate.Date == selectedDay.MenuDate.Date
                    })
                    .ToList(),
                SelectedDay = selectedDay == null
                    ? null
                    : new FoodListSelectedDayViewModel
                    {
                        MenuDate = selectedDay.MenuDate,
                        RawItemsText = selectedDay.ItemsText,
                        MenuItems = BuildMenuItems(selectedDay.ItemsText)
                    }
            });
        }

        [HttpGet("/FoodList/Months/{id:int}/Pdf")]
        public async Task<IActionResult> Pdf(int id)
        {
            var month = await _foodMenuService.GetPublishedFoodMenuMonthAsync(id);
            if (month == null || string.IsNullOrWhiteSpace(month.FileName))
            {
                return NotFound();
            }

            var downloadResult = await _foodMenuAttachmentApiClient.DownloadAsync(month.Id, month.FileName);
            if (!downloadResult.IsSuccess)
            {
                return NotFound();
            }

            return File(downloadResult.Content, "application/pdf");
        }

        private static MEC.Application.Abstractions.Service.SchoolService.Model.FoodMenuDayModel? ResolveSelectedDay(
            MEC.Application.Abstractions.Service.SchoolService.Model.FoodMenuMonthModel month,
            string? date)
        {
            if (!string.IsNullOrWhiteSpace(date) &&
                DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var selectedDate))
            {
                var exactMatch = month.Days.FirstOrDefault(x => x.MenuDate.Date == selectedDate.Date);
                if (exactMatch != null)
                {
                    return exactMatch;
                }
            }

            return month.Days
                .OrderBy(x => x.MenuDate)
                .FirstOrDefault();
        }

        private static string BuildMonthLabel(int year, int month)
        {
            return new DateTime(year, month, 1).ToString("MMMM yyyy", new CultureInfo("tr-TR"));
        }

        private static List<FoodListMenuLineViewModel> BuildMenuItems(string? itemsText)
        {
            if (string.IsNullOrWhiteSpace(itemsText))
            {
                return new List<FoodListMenuLineViewModel>();
            }

            var culture = new CultureInfo("tr-TR");
            return itemsText
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => new FoodListMenuLineViewModel
                {
                    Text = line.ToUpper(culture),
                    IsSectionHeading = IsSectionHeading(line)
                })
                .ToList();
        }

        private static bool IsSectionHeading(string line)
        {
            var normalized = NormalizeForMatch(line);
            return normalized is "kahvalti" or "ogle yemegi" or "salata bar" or "ikindi";
        }

        private static string NormalizeForMatch(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var normalized = text.Trim().Normalize(NormalizationForm.FormD);
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
                    _ => lower
                });
            }

            return builder.ToString()
                .Replace("ğ", "g")
                .Replace("ü", "u")
                .Replace("ö", "o")
                .Replace("ş", "s")
                .Replace("ç", "c");
        }
    }
}
