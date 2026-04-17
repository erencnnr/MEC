using System.Globalization;
using ClosedXML.Excel;
using MEC.Application.Abstractions.Service.LeaveService;
using MEC.Application.Abstractions.Service.LeaveService.Model;
using MEC.Domain.Common;
using MEC.Domain.Common.Enum;
using MEC.Domain.Entity.Leave;
using MEC.Portal.Models;
using MEC.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace MEC.Portal.Controllers
{
    public class LeaveController : Controller
    {
        private const int DefaultPageSize = 10;
        private const long MaxAttachmentSizeBytes = 10 * 1024 * 1024;
        private const long MaxBulkLeaveUploadSizeBytes = 10 * 1024 * 1024;
        private const string BulkLeaveUploadMethodName = "BulkLeaveUpload";
        private const string BulkLeaveErrorReportCachePrefix = "bulk-leave-error-report:";

        private static readonly string[] SupportedDateFormats =
        {
            "d.M.yyyy H:mm",
            "d.M.yyyy HH:mm",
            "dd.MM.yyyy H:mm",
            "dd.MM.yyyy HH:mm",
            "d.M.yyyy",
            "dd.MM.yyyy"
        };

        private static readonly HashSet<string> AllowedAttachmentExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf",
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".txt",
            ".doc",
            ".docx",
            ".xls",
            ".xlsx"
        };

        private readonly IAttachmentApiClient _attachmentApiClient;
        private readonly ILeaveService _leaveService;
        private readonly IMemoryCache _memoryCache;

        public LeaveController(
            IAttachmentApiClient attachmentApiClient,
            ILeaveService leaveService,
            IMemoryCache memoryCache)
        {
            _attachmentApiClient = attachmentApiClient;
            _leaveService = leaveService;
            _memoryCache = memoryCache;
        }

        [HttpGet]
        public async Task<IActionResult> RequestLeave()
        {
            var model = new LeaveRequestViewModel();
            await PopulateLeaveRequestOptionsAsync(model);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> History(
            int? year = null,
            int? status = null,
            int? leaveTypeId = null,
            string sort = "created_desc",
            int page = 1)
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _leaveService.GetLeaveHistoryAsync(new LeaveHistoryQueryModel
            {
                UserEmail = userEmail,
                Year = year,
                Status = status,
                LeaveTypeId = leaveTypeId,
                Sort = sort,
                Page = page,
                PageSize = DefaultPageSize
            });

            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                ViewBag.Error = result.ErrorMessage;
            }

            var historyViewModel = new LeaveHistoryViewModel
            {
                LeaveHistory = result.Items.Select(MapLeaveHistoryItem).ToList(),
                YearOptions = result.YearOptions,
                LeaveTypeOptions = result.LeaveTypeOptions.Select(MapLeaveTypeOption).ToList(),
                StatusOptions = result.StatusOptions.Select(MapLeaveStatusOption).ToList(),
                SelectedYear = result.SelectedYear,
                SelectedStatus = result.SelectedStatus,
                SelectedLeaveTypeId = result.SelectedLeaveTypeId,
                SelectedSort = result.SelectedSort,
                CurrentPage = result.CurrentPage,
                TotalPages = result.TotalPages,
                TotalCount = result.TotalCount,
                PageSize = result.PageSize
            };

            return View(historyViewModel);
        }

        [HttpGet("/Leave/History/{id:int}")]
        public async Task<IActionResult> HistoryDetail(int id)
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var leave = await _leaveService.GetLeaveHistoryDetailAsync(userEmail, id);
            if (leave == null)
            {
                return NotFound();
            }

            return View(new LeaveHistoryDetailViewModel
            {
                Item = MapLeaveHistoryItem(leave)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestLeave(LeaveRequestViewModel model)
        {
            await PopulateLeaveRequestOptionsAsync(model);

            if (!TryParseDate(model.StartDate, out var startDate))
            {
                ModelState.AddModelError(nameof(model.StartDate), "Başlangıç tarihi geçersiz.");
            }

            if (!TryParseDate(model.EndDate, out var endDate))
            {
                ModelState.AddModelError(nameof(model.EndDate), "Bitiş tarihi geçersiz.");
            }

            LeaveRequestValidationModel? validation = null;
            if (ModelState.IsValid)
            {
                validation = await _leaveService.ValidateLeaveRequestAsync(new LeaveRequestCreateModel
                {
                    UserEmail = User.Identity?.Name ?? string.Empty,
                    StartDate = startDate,
                    EndDate = endDate,
                    LeaveTypeId = model.LeaveTypeId ?? 0,
                    Reason = model.Reason
                });

                model.RequestedDays = validation.RequestedDays;

                foreach (var fieldError in validation.FieldErrors)
                {
                    var fieldName = fieldError.Key switch
                    {
                        "StartDate" => nameof(model.StartDate),
                        "EndDate" => nameof(model.EndDate),
                        "Reason" => nameof(model.Reason),
                        "LeaveTypeId" => nameof(model.LeaveTypeId),
                        _ => string.Empty
                    };

                    ModelState.AddModelError(fieldName, fieldError.Value);
                }
            }

            if (model.Attachment != null && model.Attachment.Length > 0)
            {
                if (model.Attachment.Length > MaxAttachmentSizeBytes)
                {
                    ModelState.AddModelError(nameof(model.Attachment), "Ek dosya boyutu 10 MB sınırını aşamaz.");
                }

                var fileExtension = Path.GetExtension(model.Attachment.FileName);
                if (string.IsNullOrWhiteSpace(fileExtension) || !AllowedAttachmentExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError(nameof(model.Attachment), "Sadece PDF, görsel veya ofis dosyaları yüklenebilir.");
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

            var createResult = await _leaveService.CreateLeaveRequestAsync(new LeaveRequestCreateModel
            {
                UserEmail = userEmail,
                StartDate = startDate,
                EndDate = endDate,
                LeaveTypeId = model.LeaveTypeId ?? 0,
                RequestedDays = model.RequestedDays,
                Reason = model.Reason
            });

            if (!createResult.IsSuccess || createResult.Data == null)
            {
                ModelState.AddModelError(string.Empty, createResult.Message);
                return View(model);
            }

            if (model.Attachment != null && model.Attachment.Length > 0)
            {
                var uploadResult = await _attachmentApiClient.UploadLeaveAttachmentAsync(createResult.Data.LeaveId, model.Attachment);
                if (!uploadResult.IsSuccess)
                {
                    await _leaveService.DeleteLeaveAsync(createResult.Data.LeaveId);
                    ModelState.AddModelError(nameof(model.Attachment), "Ek dosya yüklenemedi. " + uploadResult.Message);
                    return View(model);
                }
            }

            TempData["LeaveSuccess"] = createResult.Message;
            return RedirectToAction(nameof(RequestLeave));
        }

        [HttpPost("/Leave/BulkLeaveUpload")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkLeaveUpload(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(CreateBulkLeaveResponse(false, "danger", "Yüklenecek Excel dosyası seçiniz.", 0, 0, null, Array.Empty<BulkLeaveUpdatedUserModel>()));
            }

            if (file.Length > MaxBulkLeaveUploadSizeBytes)
            {
                return Json(CreateBulkLeaveResponse(false, "danger", "Excel dosyası 10 MB sınırını aşamaz.", 0, 0, null, Array.Empty<BulkLeaveUpdatedUserModel>()));
            }

            var extension = Path.GetExtension(file.FileName);
            if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return Json(CreateBulkLeaveResponse(false, "danger", "Sadece .xlsx uzantılı Excel dosyası yükleyebilirsiniz.", 0, 0, null, Array.Empty<BulkLeaveUpdatedUserModel>()));
            }

            using var stream = file.OpenReadStream();
            var result = await _leaveService.BulkUploadLeaveDaysAsync(new BulkLeaveUploadRequestModel
            {
                ExcelStream = stream,
                CurrentUser = User.Identity?.Name ?? "anonymous",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                MethodName = BulkLeaveUploadMethodName
            });

            var errorReportUrl = result.FailedRows.Count > 0
                ? CreateBulkLeaveErrorReportUrl(result.FailedRows)
                : null;

            return Json(CreateBulkLeaveResponse(result, errorReportUrl));
        }

        [HttpGet("/Leave/BulkLeaveUpload/ErrorReport/{token}")]
        public IActionResult DownloadBulkLeaveErrorReport(string token)
        {
            if (string.IsNullOrWhiteSpace(token) ||
                !_memoryCache.TryGetValue(GetBulkLeaveErrorReportCacheKey(token), out byte[]? reportBytes) ||
                reportBytes == null)
            {
                return NotFound();
            }

            var fileName = $"toplu-izin-hata-raporu-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";
            return File(
                reportBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        [HttpGet("/Leave/BulkLeaveUpload/Template")]
        public IActionResult DownloadBulkLeaveUploadTemplate()
        {
            var templateBytes = CreateBulkLeaveUploadTemplate();

            return File(
                templateBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "toplu-izin-yukleme-sablonu.xlsx");
        }

        private async Task<List<LeaveTypeOptionViewModel>> GetActiveLeaveTypeOptionsAsync()
        {
            var leaveTypes = await _leaveService.GetActiveLeaveTypeOptionsAsync();

            return leaveTypes
                .Select(MapLeaveTypeOption)
                .ToList();
        }

        private async Task PopulateLeaveRequestOptionsAsync(LeaveRequestViewModel model)
        {
            model.LeaveTypes = await GetActiveLeaveTypeOptionsAsync();
            model.Holidays = (await _leaveService.GetHolidayCalendarItemsAsync())
                .Select(MapHolidayCalendarItem)
                .ToList();
        }

        private static LeaveTypeOptionViewModel MapLeaveTypeOption(LeaveTypeOptionModel option)
        {
            return new LeaveTypeOptionViewModel
            {
                Id = option.Id,
                Name = option.Name,
                Code = option.Code
            };
        }

        private static HolidayCalendarItemViewModel MapHolidayCalendarItem(HolidayCalendarItemModel option)
        {
            return new HolidayCalendarItemViewModel
            {
                Name = option.Name,
                StartDate = option.StartDate,
                EndDate = option.EndDate
            };
        }

        private static LeaveStatusFilterOptionViewModel MapLeaveStatusOption(LeaveStatusOptionModel option)
        {
            return new LeaveStatusFilterOptionViewModel
            {
                Value = option.Value,
                Label = option.Label
            };
        }

        private static LeaveHistoryItemViewModel MapLeaveHistoryItem(LeaveHistoryItemModel item)
        {
            return new LeaveHistoryItemViewModel
            {
                Id = item.Id,
                LeaveTypeId = item.LeaveTypeId,
                LeaveType = item.LeaveType,
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                RequestedDays = item.RequestedDays,
                Reason = item.Reason,
                Status = item.Status,
                RemainingLeaveDays = item.RemainingLeaveDays,
                CreatedDate = item.CreatedDate,
                StatusLabel = item.StatusLabel,
                StatusTone = item.StatusTone,
                DecisionDisplay = item.DecisionDisplay
            };
        }

        private static LeaveHistoryViewModel CreateEmptyHistoryViewModel(
            int currentYear,
            int? selectedStatus,
            int? selectedLeaveTypeId,
            string selectedSort,
            List<LeaveTypeOptionViewModel> leaveTypeOptions)
        {
            return new LeaveHistoryViewModel
            {
                YearOptions = Enumerable.Range(1970, currentYear - 1969).Reverse().ToList(),
                LeaveTypeOptions = leaveTypeOptions,
                StatusOptions = CreateStatusOptions(),
                SelectedStatus = selectedStatus,
                SelectedLeaveTypeId = selectedLeaveTypeId,
                SelectedSort = selectedSort,
                CurrentPage = 1,
                TotalPages = 1,
                TotalCount = 0,
                PageSize = DefaultPageSize
            };
        }

        private static List<int> CreateYearOptions(List<Leave> leaves, int currentYear)
        {
            if (!leaves.Any())
            {
                return Enumerable.Range(1970, currentYear - 1969).Reverse().ToList();
            }

            var oldestYear = leaves.Min(x => Math.Min(x.StartDate.Year, x.EndDate.Year));
            return Enumerable.Range(oldestYear, currentYear - oldestYear + 1).ToList();
        }

        private static List<LeaveStatusFilterOptionViewModel> CreateStatusOptions()
        {
            return new List<LeaveStatusFilterOptionViewModel>
            {
                new() { Value = (int)LeaveStatus.Pending, Label = "Onay Bekliyor" },
                new() { Value = (int)LeaveStatus.Approved, Label = "Onaylandı" },
                new() { Value = (int)LeaveStatus.Rejected, Label = "Reddedildi" },
                new() { Value = (int)LeaveStatus.Cancelled, Label = "İptal" }
            };
        }

        private static LeaveHistoryItemViewModel MapLeaveHistoryItem(Leave leave)
        {
            return new LeaveHistoryItemViewModel
            {
                Id = leave.Id,
                LeaveTypeId = leave.LeaveTypeId,
                LeaveType = GetLeaveTypeName(leave),
                StartDate = leave.StartDate,
                EndDate = leave.EndDate,
                RequestedDays = GetRequestedDays(leave),
                Reason = leave.Reason,
                Status = leave.Status,
                RemainingLeaveDays = GetRemainingLeaveDays(leave),
                CreatedDate = leave.CreatedDate,
                StatusLabel = GetStatusLabel(leave.Status),
                StatusTone = GetStatusTone(leave.Status),
                DecisionDisplay = GetDecisionDisplay(leave)
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

        private static decimal GetRequestedDays(Leave leave)
        {
            if (leave.RequestedDays > 0)
            {
                return leave.RequestedDays;
            }

            return LeaveDurationCalculator.CalculateRequestedDays(leave.StartDate, leave.EndDate);
        }

        private static string GetLeaveTypeName(Leave leave)
        {
            return leave.LeaveType?.Name ?? string.Empty;
        }

        private static decimal GetRemainingLeaveDays(Leave leave)
        {
            return leave.RemainingLeaveDays;
        }

        private static LeaveStatus ToLeaveStatus(int status)
        {
            return Enum.IsDefined(typeof(LeaveStatus), status)
                ? (LeaveStatus)status
                : LeaveStatus.Pending;
        }

        private static string GetStatusLabel(int status)
        {
            return ToLeaveStatus(status) switch
            {
                LeaveStatus.Approved => "Onaylandı",
                LeaveStatus.Rejected => "Reddedildi",
                LeaveStatus.Cancelled => "İptal",
                _ => "Onay Bekliyor"
            };
        }

        private static string GetStatusTone(int status)
        {
            return ToLeaveStatus(status) switch
            {
                LeaveStatus.Approved => "approved",
                LeaveStatus.Rejected => "rejected",
                LeaveStatus.Cancelled => "cancelled",
                _ => "pending"
            };
        }

        private static string GetDecisionDisplay(Leave leave)
        {
            var propertyNames = new[]
            {
                "ApprovedBy",
                "RejectedBy",
                "CancelledBy",
                "CanceledBy",
                "DecisionBy",
                "UpdatedBy"
            };

            foreach (var propertyName in propertyNames)
            {
                var propertyValue = leave.GetType().GetProperty(propertyName)?.GetValue(leave)?.ToString();
                if (!string.IsNullOrWhiteSpace(propertyValue))
                {
                    return propertyValue;
                }
            }

            return leave.Status == (int)LeaveStatus.Pending ? "-" : "Belirtilmedi";
        }

        private string CreateBulkLeaveErrorReportUrl(IReadOnlyCollection<BulkLeaveUploadErrorRowModel> errorRows)
        {
            var token = Guid.NewGuid().ToString("N");
            var reportBytes = CreateBulkLeaveErrorReport(errorRows);

            _memoryCache.Set(
                GetBulkLeaveErrorReportCacheKey(token),
                reportBytes,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                });

            return $"/Leave/BulkLeaveUpload/ErrorReport/{Uri.EscapeDataString(token)}";
        }

        private static byte[] CreateBulkLeaveErrorReport(IReadOnlyCollection<BulkLeaveUploadErrorRowModel> errorRows)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Hatalı Satırlar");

            worksheet.Cell(1, 1).Value = "Satır No";
            worksheet.Cell(1, 2).Value = "Email";
            worksheet.Cell(1, 3).Value = "Eklenecek İzin Gün Sayısı";
            worksheet.Cell(1, 4).Value = "Açıklama";
            worksheet.Cell(1, 5).Value = "Hata Nedeni";

            var headerRange = worksheet.Range(1, 1, 1, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#fff4d6");
            headerRange.Style.Font.FontColor = XLColor.FromHtml("#18285c");

            var rowIndex = 2;
            foreach (var errorRow in errorRows)
            {
                worksheet.Cell(rowIndex, 1).Value = errorRow.RowNumber;
                worksheet.Cell(rowIndex, 2).Value = errorRow.Email;
                worksheet.Cell(rowIndex, 3).Value = errorRow.RawDays;
                worksheet.Cell(rowIndex, 4).Value = errorRow.Description;
                worksheet.Cell(rowIndex, 5).Value = errorRow.ErrorMessage;
                rowIndex++;
            }

            worksheet.Columns().AdjustToContents();
            worksheet.SheetView.FreezeRows(1);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static byte[] CreateBulkLeaveUploadTemplate()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Toplu İzin Yükleme");

            worksheet.Cell(1, 1).Value = "email";
            worksheet.Cell(1, 2).Value = "eklenecek izin gün sayısı";
            worksheet.Cell(1, 3).Value = "açıklama";

            worksheet.Cell(2, 1).Value = "ornek@domain.com";
            worksheet.Cell(2, 2).Value = 1.5;
            worksheet.Cell(2, 3).Value = "Açıklama örneği";

            var headerRange = worksheet.Range(1, 1, 1, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#e9f7ef");
            headerRange.Style.Font.FontColor = XLColor.FromHtml("#18285c");

            worksheet.Column(2).Style.NumberFormat.Format = "0.##";
            worksheet.Columns().AdjustToContents();
            worksheet.SheetView.FreezeRows(1);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static string GetBulkLeaveErrorReportCacheKey(string token)
        {
            return BulkLeaveErrorReportCachePrefix + token;
        }

        private static object CreateBulkLeaveResponse(
            bool success,
            string level,
            string message,
            int updatedCount,
            int failedCount,
            string? errorReportUrl,
            IReadOnlyCollection<BulkLeaveUpdatedUserModel> updatedUsers)
        {
            return new
            {
                success,
                level,
                message,
                updatedCount,
                failedCount,
                errorReportUrl,
                updatedUsers = updatedUsers.Select(x => new
                {
                    email = x.Email,
                    newLeaveDays = x.NewLeaveDays
                })
            };
        }

        private static object CreateBulkLeaveResponse(BulkLeaveUploadResultModel result, string? errorReportUrl)
        {
            return CreateBulkLeaveResponse(
                result.Success,
                result.Level,
                result.Message,
                result.UpdatedCount,
                result.FailedCount,
                errorReportUrl,
                result.UpdatedUsers);
        }

    }
}
