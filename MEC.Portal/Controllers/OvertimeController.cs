using MEC.Application.Abstractions.Service.OvertimeService;
using MEC.Application.Abstractions.Service.OvertimeService.Model;
using MEC.Domain.Common.Enum;
using MEC.Portal.Models;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace MEC.Portal.Controllers
{
    public class OvertimeController : Controller
    {
        private const int DefaultPageSize = 10;

        private static readonly string[] SupportedDateFormats =
        {
            "d.M.yyyy",
            "dd.MM.yyyy"
        };

        private readonly IOvertimeService _overtimeService;

        public OvertimeController(IOvertimeService overtimeService)
        {
            _overtimeService = overtimeService;
        }

        [HttpGet]
        [ActionName("Request")]
        public IActionResult RequestPage()
        {
            return View(new OvertimeRequestViewModel());
        }

        [HttpGet]
        public async Task<IActionResult> History(
            int? year = null,
            int? status = null,
            string sort = "created_desc",
            int page = 1)
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _overtimeService.GetOvertimeHistoryAsync(new OvertimeHistoryQueryModel
            {
                UserEmail = userEmail,
                Year = year,
                Status = status,
                Sort = sort,
                Page = page,
                PageSize = DefaultPageSize
            });

            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                ViewBag.Error = result.ErrorMessage;
            }

            return View(new OvertimeHistoryViewModel
            {
                Items = result.Items.Select(MapHistoryItem).ToList(),
                YearOptions = result.YearOptions,
                StatusOptions = result.StatusOptions.Select(MapStatusOption).ToList(),
                SelectedYear = result.SelectedYear,
                SelectedStatus = result.SelectedStatus,
                SelectedSort = result.SelectedSort,
                CurrentPage = result.CurrentPage,
                TotalPages = result.TotalPages,
                TotalCount = result.TotalCount,
                PageSize = result.PageSize
            });
        }

        [HttpGet("/Overtime/History/{id:int}")]
        public async Task<IActionResult> HistoryDetail(int id)
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var item = await _overtimeService.GetOvertimeHistoryDetailAsync(userEmail, id);
            if (item == null)
            {
                return NotFound();
            }

            return View(new OvertimeHistoryDetailViewModel
            {
                Item = MapHistoryItem(item)
            });
        }

        [HttpPost("/Overtime/Request")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestPage(OvertimeRequestViewModel model)
        {
            if (!TryParseDate(model.Date, out var date))
            {
                ModelState.AddModelError(nameof(model.Date), "Mesai tarihi geçersiz.");
            }

            if (!TryParseTime(model.StartTime, out var startTime))
            {
                ModelState.AddModelError(nameof(model.StartTime), "Başlangıç saati geçersiz.");
            }

            if (!TryParseTime(model.EndTime, out var endTime))
            {
                ModelState.AddModelError(nameof(model.EndTime), "Bitiş saati geçersiz.");
            }

            var startDate = date.Date.Add(startTime);
            var endDate = date.Date.Add(endTime);

            OvertimeRequestValidationModel? validation = null;
            if (ModelState.IsValid)
            {
                validation = await _overtimeService.ValidateOvertimeRequestAsync(new OvertimeRequestCreateModel
                {
                    UserEmail = User.Identity?.Name ?? string.Empty,
                    StartDate = startDate,
                    EndDate = endDate,
                    Reason = model.Reason
                });

                model.RequestedHours = validation.RequestedHours;

                foreach (var fieldError in validation.FieldErrors)
                {
                    var fieldName = fieldError.Key switch
                    {
                        "Date" => nameof(model.Date),
                        "StartTime" => nameof(model.StartTime),
                        "EndTime" => nameof(model.EndTime),
                        "Reason" => nameof(model.Reason),
                        _ => string.Empty
                    };

                    ModelState.AddModelError(fieldName, fieldError.Value);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userEmail = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var createResult = await _overtimeService.CreateOvertimeRequestAsync(new OvertimeRequestCreateModel
            {
                UserEmail = userEmail,
                StartDate = startDate,
                EndDate = endDate,
                Reason = model.Reason
            });

            if (!createResult.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, createResult.Message);
                return View(model);
            }

            TempData["OvertimeSuccess"] = createResult.Message;
            return RedirectToAction("Request");
        }

        private static OvertimeHistoryItemViewModel MapHistoryItem(OvertimeHistoryItemModel item)
        {
            return new OvertimeHistoryItemViewModel
            {
                Id = item.Id,
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                RequestedHours = item.RequestedHours,
                Reason = item.Reason,
                Status = item.Status,
                CreatedDate = item.CreatedDate,
                StatusLabel = item.StatusLabel,
                StatusTone = item.StatusTone,
                DecisionDisplay = item.DecisionDisplay
            };
        }

        private static OvertimeStatusFilterOptionViewModel MapStatusOption(OvertimeStatusOptionModel option)
        {
            return new OvertimeStatusFilterOptionViewModel
            {
                Value = option.Value,
                Label = option.Label
            };
        }

        private static bool TryParseDate(string? value, out DateTime date)
        {
            return DateTime.TryParseExact(
                value,
                SupportedDateFormats,
                CultureInfo.GetCultureInfo("tr-TR"),
                DateTimeStyles.AllowWhiteSpaces,
                out date);
        }

        private static bool TryParseTime(string? value, out TimeSpan time)
        {
            return TimeSpan.TryParseExact(value, @"hh\:mm", CultureInfo.InvariantCulture, out time);
        }
    }
}
