using System;
using System.Globalization;
using System.Linq;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Employee;
using MEC.Domain.Entity.Leave;
using MEC.Portal.Models;
using Microsoft.AspNetCore.Mvc;

namespace MEC.Portal.Controllers
{
    public class LeaveController : Controller
    {
        private const int DefaultPageSize = 10;
        private static readonly string[] SupportedDateFormats = { "d.M.yyyy", "dd.MM.yyyy" };
        private static readonly string[] AllowedLeaveTypes =
        {
            "Yıllık İzin",
            "Hastalık İzni",
            "Mazeret İzni",
            "Ücretsiz İzin",
            "Diğer"
        };

        private readonly IGenericRepository<Leave> _leaveRepository;
        private readonly IGenericRepository<Employee> _employeeRepository;

        public LeaveController(
            IGenericRepository<Leave> leaveRepository,
            IGenericRepository<Employee> employeeRepository)
        {
            _leaveRepository = leaveRepository;
            _employeeRepository = employeeRepository;
        }

        [HttpGet]
        public IActionResult RequestLeave()
        {
            return View(new LeaveRequestViewModel());
        }

        [HttpGet]
        public async Task<IActionResult> History(
            int? year = null,
            int? status = null,
            string? leaveType = null,
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
            var selectedLeaveType = string.IsNullOrWhiteSpace(leaveType) ? string.Empty : leaveType.Trim();

            var employee = (await _employeeRepository.GetAllAsync(x => x.Email == userEmail && !x.IsDeleted)).FirstOrDefault();
            if (employee == null)
            {
                ViewBag.Error = "Kullanıcı kaydı bulunamadı.";
                return View(CreateEmptyHistoryViewModel(currentYear, status, selectedLeaveType, selectedSort));
            }

            var employeeLeaves = (await _leaveRepository.GetAllAsync(x => x.EmployeeId == employee.Id))
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

            if (!string.IsNullOrWhiteSpace(selectedLeaveType))
            {
                filteredItems = filteredItems.Where(x => string.Equals(x.LeaveType, selectedLeaveType, StringComparison.OrdinalIgnoreCase));
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
                LeaveTypeOptions = allItems
                    .Select(x => x.LeaveType)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),
                StatusOptions = CreateStatusOptions(),
                SelectedYear = year,
                SelectedStatus = status,
                SelectedLeaveType = selectedLeaveType,
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

            var leave = (await _leaveRepository.GetAllAsync(x => x.EmployeeId == employee.Id && x.Id == id)).FirstOrDefault();
            if (leave == null)
            {
                return NotFound();
            }

            var model = new LeaveHistoryDetailViewModel
            {
                Item = MapLeaveHistoryItem(leave)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestLeave(LeaveRequestViewModel model)
        {
            if (!TryParseDate(model.StartDate, out var startDate))
            {
                ModelState.AddModelError(nameof(model.StartDate), "Baslangic tarihi gecersiz.");
            }

            if (!TryParseDate(model.EndDate, out var endDate))
            {
                ModelState.AddModelError(nameof(model.EndDate), "Bitis tarihi gecersiz.");
            }

            if (ModelState.IsValid && endDate < startDate)
            {
                ModelState.AddModelError(nameof(model.EndDate), "Bitis tarihi baslangic tarihinden once olamaz.");
            }

            if (string.IsNullOrWhiteSpace(model.Reason))
            {
                ModelState.AddModelError(nameof(model.Reason), "Izin nedeni zorunludur.");
            }

            if (string.IsNullOrWhiteSpace(model.LeaveType) || !AllowedLeaveTypes.Contains(model.LeaveType))
            {
                ModelState.AddModelError(nameof(model.LeaveType), "Gecerli bir izin turu seciniz.");
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
                ModelState.AddModelError(string.Empty, "Kullanici kaydi bulunamadi.");
                return View(model);
            }

            var leaveRequest = new Leave
            {
                EmployeeId = employee.Id,
                StartDate = startDate,
                EndDate = endDate,
                LeaveType = model.LeaveType.Trim(),
                RequestedDays = (endDate.Date - startDate.Date).Days + 1,
                Reason = model.Reason.Trim(),
                Status = 0,
                CreatedDate = DateTime.Now
            };

            await _leaveRepository.AddAsync(leaveRequest);

            TempData["LeaveSuccess"] = "Izin talebiniz basariyla gonderildi.";
            return RedirectToAction(nameof(RequestLeave));
        }

        private static LeaveHistoryViewModel CreateEmptyHistoryViewModel(
            int currentYear,
            int? selectedStatus,
            string selectedLeaveType,
            string selectedSort)
        {
            return new LeaveHistoryViewModel
            {
                YearOptions = Enumerable.Range(1970, currentYear - 1969).Reverse().ToList(),
                LeaveTypeOptions = new List<string>(),
                StatusOptions = CreateStatusOptions(),
                SelectedStatus = selectedStatus,
                SelectedLeaveType = selectedLeaveType,
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
                new() { Value = 0, Label = "Onay Bekliyor" },
                new() { Value = 1, Label = "Onaylandı" },
                new() { Value = 2, Label = "Reddedildi" },
                new() { Value = 3, Label = "İptal" }
            };
        }

        private static LeaveHistoryItemViewModel MapLeaveHistoryItem(Leave leave)
        {
            return new LeaveHistoryItemViewModel
            {
                Id = leave.Id,
                LeaveType = GetLeaveType(leave),
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
                DateTimeStyles.None,
                out date);
        }

        private static int GetRequestedDays(Leave leave)
        {
            var requestedDaysProperty = leave.GetType().GetProperty("RequestedDays")?.GetValue(leave);
            if (requestedDaysProperty is int requestedDays && requestedDays > 0)
            {
                return requestedDays;
            }

            return (leave.EndDate.Date - leave.StartDate.Date).Days + 1;
        }

        private static string GetLeaveType(Leave leave)
        {
            return leave.GetType().GetProperty("LeaveType")?.GetValue(leave)?.ToString() ?? string.Empty;
        }

        private static int GetRemainingLeaveDays(Leave leave)
        {
            var remainingLeaveDaysProperty = leave.GetType().GetProperty("RemainingLeaveDays")?.GetValue(leave);
            return remainingLeaveDaysProperty is int remainingLeaveDays ? remainingLeaveDays : 0;
        }

        private static string GetStatusLabel(int status)
        {
            return status switch
            {
                1 => "Onaylandı",
                2 => "Reddedildi",
                3 => "İptal",
                _ => "Onay Bekliyor"
            };
        }

        private static string GetStatusTone(int status)
        {
            return status switch
            {
                1 => "approved",
                2 => "rejected",
                3 => "cancelled",
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

            return leave.Status == 0 ? "-" : "Belirtilmedi";
        }
    }
}
