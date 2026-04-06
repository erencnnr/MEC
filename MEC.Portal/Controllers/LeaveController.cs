using System.Globalization;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Common;
using MEC.Domain.Common.Enum;
using MEC.Domain.Entity.Employee;
using MEC.Domain.Entity.Leave;
using MEC.Portal.Models;
using MEC.Portal.Services;
using Microsoft.AspNetCore.Mvc;

namespace MEC.Portal.Controllers
{
    public class LeaveController : Controller
    {
        private const int DefaultPageSize = 10;
        private const long MaxAttachmentSizeBytes = 10 * 1024 * 1024;

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

        private readonly IGenericRepository<Leave> _leaveRepository;
        private readonly IGenericRepository<Employee> _employeeRepository;
        private readonly IGenericRepository<LeaveType> _leaveTypeRepository;
        private readonly IAttachmentApiClient _attachmentApiClient;

        public LeaveController(
            IGenericRepository<Leave> leaveRepository,
            IGenericRepository<Employee> employeeRepository,
            IGenericRepository<LeaveType> leaveTypeRepository,
            IAttachmentApiClient attachmentApiClient)
        {
            _leaveRepository = leaveRepository;
            _employeeRepository = employeeRepository;
            _leaveTypeRepository = leaveTypeRepository;
            _attachmentApiClient = attachmentApiClient;
        }

        [HttpGet]
        public async Task<IActionResult> RequestLeave()
        {
            var model = new LeaveRequestViewModel
            {
                LeaveTypes = await GetActiveLeaveTypeOptionsAsync()
            };

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

            var currentYear = DateTime.Today.Year;
            var selectedSort = string.Equals(sort, "created_asc", StringComparison.OrdinalIgnoreCase)
                ? "created_asc"
                : "created_desc";
            var leaveTypeOptions = await GetActiveLeaveTypeOptionsAsync();

            var employee = (await _employeeRepository.GetAllAsync(x => x.Email == userEmail && !x.IsDeleted)).FirstOrDefault();
            if (employee == null)
            {
                ViewBag.Error = "Kullanıcı kaydı bulunamadı.";
                return View(CreateEmptyHistoryViewModel(currentYear, status, leaveTypeId, selectedSort, leaveTypeOptions));
            }

            var employeeLeaves = (await _leaveRepository.GetAllAsync(x => x.EmployeeId == employee.Id, x => x.LeaveType))
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.Id)
                .ToList();

            var allItems = employeeLeaves.Select(MapLeaveHistoryItem).ToList();
            var filteredItems = allItems.AsEnumerable();

            if (year.HasValue)
            {
                filteredItems = filteredItems.Where(x => x.StartDate.Year == year.Value || x.EndDate.Year == year.Value);
            }

            if (status.HasValue)
            {
                filteredItems = filteredItems.Where(x => x.Status == status.Value);
            }

            if (leaveTypeId.HasValue)
            {
                filteredItems = filteredItems.Where(x => x.LeaveTypeId == leaveTypeId.Value);
            }

            filteredItems = selectedSort == "created_asc"
                ? filteredItems.OrderBy(x => x.CreatedDate ?? DateTime.MinValue).ThenBy(x => x.Id)
                : filteredItems.OrderByDescending(x => x.CreatedDate ?? DateTime.MinValue).ThenByDescending(x => x.Id);

            var totalCount = filteredItems.Count();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)DefaultPageSize));
            var currentPage = Math.Min(Math.Max(page, 1), totalPages);

            var pagedItems = filteredItems
                .Skip((currentPage - 1) * DefaultPageSize)
                .Take(DefaultPageSize)
                .ToList();

            var historyViewModel = new LeaveHistoryViewModel
            {
                LeaveHistory = pagedItems,
                YearOptions = CreateYearOptions(employeeLeaves, currentYear),
                LeaveTypeOptions = leaveTypeOptions,
                StatusOptions = CreateStatusOptions(),
                SelectedYear = year,
                SelectedStatus = status,
                SelectedLeaveTypeId = leaveTypeId,
                SelectedSort = selectedSort,
                CurrentPage = currentPage,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = DefaultPageSize
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

            var employee = (await _employeeRepository.GetAllAsync(x => x.Email == userEmail && !x.IsDeleted)).FirstOrDefault();
            if (employee == null)
            {
                return RedirectToAction(nameof(History));
            }

            var leave = (await _leaveRepository.GetAllAsync(x => x.EmployeeId == employee.Id && x.Id == id, x => x.LeaveType)).FirstOrDefault();
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
            model.LeaveTypes = await GetActiveLeaveTypeOptionsAsync();

            if (!TryParseDate(model.StartDate, out var startDate))
            {
                ModelState.AddModelError(nameof(model.StartDate), "Başlangıç tarihi geçersiz.");
            }

            if (!TryParseDate(model.EndDate, out var endDate))
            {
                ModelState.AddModelError(nameof(model.EndDate), "Bitiş tarihi geçersiz.");
            }

            if (ModelState.IsValid && endDate < startDate)
            {
                ModelState.AddModelError(nameof(model.EndDate), "Bitiş tarihi başlangıç tarihinden önce olamaz.");
            }

            if (ModelState.IsValid && !LeaveDurationCalculator.IsWithinWorkingHours(startDate))
            {
                ModelState.AddModelError(nameof(model.StartDate), "Başlangıç saati 09:00 ile 18:00 arasında olmalıdır.");
            }

            if (ModelState.IsValid && !LeaveDurationCalculator.IsWithinWorkingHours(endDate))
            {
                ModelState.AddModelError(nameof(model.EndDate), "Bitiş saati 09:00 ile 18:00 arasında olmalıdır.");
            }

            if (ModelState.IsValid)
            {
                model.RequestedDays = LeaveDurationCalculator.CalculateRequestedDays(startDate, endDate);

                if (model.RequestedDays <= 0)
                {
                    ModelState.AddModelError(nameof(model.EndDate), "Seçilen tarih ve saat aralığı için kullanılabilir izin günü hesaplanamadı.");
                }
            }

            if (string.IsNullOrWhiteSpace(model.Reason))
            {
                ModelState.AddModelError(nameof(model.Reason), "İzin nedeni zorunludur.");
            }

            LeaveTypeOptionViewModel? selectedLeaveType = null;
            if (!model.LeaveTypeId.HasValue || model.LeaveTypeId.Value <= 0)
            {
                ModelState.AddModelError(nameof(model.LeaveTypeId), "Geçerli bir izin türü seçiniz.");
            }
            else
            {
                selectedLeaveType = model.LeaveTypes.FirstOrDefault(x => x.Id == model.LeaveTypeId.Value);
                if (selectedLeaveType == null)
                {
                    ModelState.AddModelError(nameof(model.LeaveTypeId), "Geçerli bir izin türü seçiniz.");
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

            var employee = (await _employeeRepository.GetAllAsync(x => x.Email == userEmail && !x.IsDeleted)).FirstOrDefault();
            if (employee == null)
            {
                ModelState.AddModelError(string.Empty, "Kullanıcı kaydı bulunamadı.");
                return View(model);
            }

            var leaveRequest = new Leave
            {
                EmployeeId = employee.Id,
                StartDate = startDate,
                EndDate = endDate,
                LeaveTypeId = selectedLeaveType!.Id,
                RequestedDays = model.RequestedDays,
                Reason = model.Reason.Trim(),
                Status = (int)LeaveStatus.Pending,
                CreatedDate = DateTime.Now
            };

            await _leaveRepository.AddAsync(leaveRequest);

            if (model.Attachment != null && model.Attachment.Length > 0)
            {
                var uploadResult = await _attachmentApiClient.UploadLeaveAttachmentAsync(leaveRequest.Id, model.Attachment);
                if (!uploadResult.IsSuccess)
                {
                    _leaveRepository.Delete(leaveRequest);
                    ModelState.AddModelError(nameof(model.Attachment), "Ek dosya yüklenemedi. " + uploadResult.Message);
                    return View(model);
                }
            }

            TempData["LeaveSuccess"] = "İzin talebiniz başarıyla gönderildi.";
            return RedirectToAction(nameof(RequestLeave));
        }

        private async Task<List<LeaveTypeOptionViewModel>> GetActiveLeaveTypeOptionsAsync()
        {
            var leaveTypes = await _leaveTypeRepository.GetAllAsync(x => x.IsActive);

            return leaveTypes
                .OrderBy(x => x.Id)
                .Select(x => new LeaveTypeOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code
                })
                .ToList();
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
    }
}
