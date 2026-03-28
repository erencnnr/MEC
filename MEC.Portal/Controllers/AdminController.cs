using Microsoft.AspNetCore.Mvc;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Application.Abstractions.Service.LeaveService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Employee;
using Microsoft.AspNetCore.Mvc.Rendering;
using MEC.Portal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEC.AssetManagementUI.Controllers
{
    public class AdminController : Controller
    {
        private static readonly string[] DefaultLeaveTypes =
        {
            "Yıllık İzin",
            "Hastalık İzni",
            "Mazeret İzni",
            "Ücretsiz İzin",
            "Diğer"
        };

        private readonly ILeaveService _leaveService;
        private readonly IAnnouncementService _announcementService;
        private readonly IGenericRepository<Employee> _employeeRepository;
        private readonly IGenericRepository<EmployeePortal> _employeePortalRepository;

        // Dependency Injection ile servisimizi içeri alıyoruz
        public AdminController(
            ILeaveService leaveService,
            IAnnouncementService announcementService,
            IGenericRepository<Employee> employeeRepository,
            IGenericRepository<EmployeePortal> employeePortalRepository)
        {
            _leaveService = leaveService;
            _announcementService = announcementService;
            _employeeRepository = employeeRepository;
            _employeePortalRepository = employeePortalRepository;
        }

        public async Task<IActionResult> Index()
        {
            var leaves = await _leaveService.GetAllLeavesAsync();
            var announcements = (await _announcementService.GetAllAnnouncementsAsync())
                .OrderByDescending(x => x.CreatedDate)
                .ToList();
            var employees = await _employeeRepository.GetAllAsync(x => !x.IsDeleted);
            var employeePortals = await _employeePortalRepository.GetAllAsync();
            var today = DateTime.Today;

            var employeeNames = employees.ToDictionary(
                x => x.Id,
                x => string.Join(" ", new[] { x.FirstName, x.LastName }.Where(y => !string.IsNullOrWhiteSpace(y))).Trim());

            var model = new AdminDashboardViewModel
            {
                AdminName = User.Identity?.Name ?? "Admin",
                GeneratedAt = DateTime.Now,
                PendingLeaveCount = leaves.Count(x => x.Status == 0),
                TotalAnnouncementCount = announcements.Count,
                TodayAnnouncementCount = announcements.Count(x => x.CreatedDate.HasValue && x.CreatedDate.Value.Date == today),
                NegativeLeaveBalanceCount = employeePortals.Count(x => x.LeaveDays < 0),
                RecentLeaveRequests = leaves
                    .Take(5)
                    .Select(x => new AdminRecentLeaveItemViewModel
                    {
                        EmployeeName = employeeNames.TryGetValue(x.EmployeeId, out var employeeName) && !string.IsNullOrWhiteSpace(employeeName)
                            ? employeeName
                            : $"#{x.EmployeeId}",
                        LeaveType = GetLeaveType(x),
                        RequestedDays = GetRequestedDays(x),
                        Status = x.Status,
                        StartDate = x.StartDate,
                        EndDate = x.EndDate,
                        CreatedDate = x.CreatedDate
                    })
                    .ToList(),
                RecentAnnouncements = announcements
                    .Take(5)
                    .Select(x => new AdminRecentAnnouncementItemViewModel
                    {
                        Title = x.Title,
                        IsActive = x.IsActive,
                        CreatedDate = x.CreatedDate
                    })
                    .ToList()
            };

            return View(model);
        }

        public IActionResult Announcements()
        {
            return View("~/Views/Announcement/Create.cshtml");
        }

        // Metodu asenkron (async) yaptık çünkü veritabanına bağlanıyoruz
        public async Task<IActionResult> LeaveRequests()
        {
            // Veritabanından tüm izinleri çekiyoruz
            var leaves = await _leaveService.GetAllLeavesAsync();
            var employees = await _employeeRepository.GetAllAsync(x => !x.IsDeleted);
            var employeeNames = employees.ToDictionary(
                x => x.Id,
                x => string.Join(" ", new[] { x.FirstName, x.LastName }.Where(y => !string.IsNullOrWhiteSpace(y))).Trim());

            var model = leaves.Select(x => new AdminLeaveRequestViewModel
            {
                Id = x.Id,
                EmployeeId = x.EmployeeId,
                EmployeeName = employeeNames.TryGetValue(x.EmployeeId, out var employeeName) && !string.IsNullOrWhiteSpace(employeeName)
                    ? employeeName
                    : $"#{x.EmployeeId}",
                LeaveType = GetLeaveType(x),
                RequestedDays = GetRequestedDays(x),
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Reason = x.Reason,
                Status = x.Status,
                RemainingLeaveDays = GetRemainingLeaveDays(x)
            }).ToList();

            // Çektiğimiz verileri ekrana (View'a) gönderiyoruz
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> LeaveReports(int? employeeId, string leaveType, DateTime? startDate, DateTime? endDate)
        {
            var model = await BuildLeaveReportModelAsync(employeeId, leaveType, startDate, endDate);
            return View(model);
        }

        [HttpGet]
        public async Task<FileResult> ExportLeaveReports(int? employeeId, string leaveType, DateTime? startDate, DateTime? endDate)
        {
            var model = await BuildLeaveReportModelAsync(employeeId, leaveType, startDate, endDate);
            var builder = new StringBuilder();

            builder.AppendLine("Calisan;Izin Turu;Baslangic Tarihi;Bitis Tarihi;Gun;Durum;Talep Tarihi;Aciklama");

            foreach (var item in model.Reports)
            {
                builder.AppendLine(string.Join(";", new[]
                {
                    EscapeCsv(item.EmployeeName),
                    EscapeCsv(item.LeaveType),
                    item.StartDate.ToString("dd.MM.yyyy"),
                    item.EndDate.ToString("dd.MM.yyyy"),
                    item.RequestedDays.ToString(),
                    EscapeCsv(GetStatusText(item.Status)),
                    item.CreatedDate?.ToString("dd.MM.yyyy") ?? string.Empty,
                    EscapeCsv(item.Reason)
                }));
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
            var fileName = $"izin-raporu-{DateTime.Now:yyyyMMdd-HHmm}.csv";

            return File(bytes, "text/csv; charset=utf-8", fileName);
        }
    
    [HttpPost] // Veri güncellediğimiz için POST kullanıyoruz
        public async Task<IActionResult> UpdateLeaveStatus(int id, int status)
        {
            // Servisimizdeki güncelleme metodunu çağırıyoruz
            var result = await _leaveService.UpdateLeaveStatusAsync(id, status);

            if (result)
            {
                // İşlem başarılıysa sayfayı yeniliyoruz
                return RedirectToAction("LeaveRequests");
            }

            // Bir hata oluştuysa hata mesajı döndürebiliriz
            return BadRequest("Durum güncellenemedi.");
        }

        private static string GetLeaveType(MEC.Domain.Entity.Leave.Leave leave)
        {
            return leave.GetType().GetProperty("LeaveType")?.GetValue(leave)?.ToString() ?? string.Empty;
        }

        private static int GetRequestedDays(MEC.Domain.Entity.Leave.Leave leave)
        {
            var requestedDaysProperty = leave.GetType().GetProperty("RequestedDays")?.GetValue(leave);
            if (requestedDaysProperty is int requestedDays && requestedDays > 0)
            {
                return requestedDays;
            }

            return (leave.EndDate.Date - leave.StartDate.Date).Days + 1;
        }

        private static int GetRemainingLeaveDays(MEC.Domain.Entity.Leave.Leave leave)
        {
            var remainingLeaveDaysProperty = leave.GetType().GetProperty("RemainingLeaveDays")?.GetValue(leave);
            return remainingLeaveDaysProperty is int remainingLeaveDays ? remainingLeaveDays : 0;
        }

        private static List<SelectListItem> BuildEmployeeOptions(IEnumerable<Employee> employees, int? selectedEmployeeId)
        {
            var items = new List<SelectListItem>
            {
                new SelectListItem { Value = string.Empty, Text = "Tüm çalışanlar", Selected = !selectedEmployeeId.HasValue }
            };

            items.AddRange(employees
                .OrderBy(x => x.FirstName)
                .ThenBy(x => x.LastName)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = string.Join(" ", new[] { x.FirstName, x.LastName }.Where(y => !string.IsNullOrWhiteSpace(y))).Trim(),
                    Selected = selectedEmployeeId.HasValue && x.Id == selectedEmployeeId.Value
                }));

            return items;
        }

        private static List<SelectListItem> BuildLeaveTypeOptions(IEnumerable<string> leaveTypes, string selectedLeaveType)
        {
            var items = new List<SelectListItem>
            {
                new SelectListItem { Value = string.Empty, Text = "Tüm izin türleri", Selected = string.IsNullOrWhiteSpace(selectedLeaveType) }
            };

            items.AddRange(leaveTypes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => new SelectListItem
                {
                    Value = x,
                    Text = x,
                    Selected = string.Equals(x, selectedLeaveType, StringComparison.OrdinalIgnoreCase)
                }));

            return items;
        }

        private static string BuildPeriodLabel(DateTime? startDate, DateTime? endDate, bool defaultedToCurrentYear)
        {
            if (startDate.HasValue && endDate.HasValue)
            {
                return defaultedToCurrentYear
                    ? $"Bu yılın tüm izinleri ({startDate.Value:dd.MM.yyyy} - {endDate.Value:dd.MM.yyyy})"
                    : $"Seçilen aralık: {startDate.Value:dd.MM.yyyy} - {endDate.Value:dd.MM.yyyy}";
            }

            if (startDate.HasValue)
            {
                return $"Başlangıç tarihi sonrası kayıtlar: {startDate.Value:dd.MM.yyyy}";
            }

            if (endDate.HasValue)
            {
                return $"Bitiş tarihine kadar olan kayıtlar: {endDate.Value:dd.MM.yyyy}";
            }

            return "Tüm izin kayıtları";
        }

        private async Task<AdminLeaveReportViewModel> BuildLeaveReportModelAsync(int? employeeId, string leaveType, DateTime? startDate, DateTime? endDate)
        {
            var leaves = await _leaveService.GetAllLeavesAsync();
            var employees = (await _employeeRepository.GetAllAsync(x => !x.IsDeleted)).ToList();
            var employeeNames = employees.ToDictionary(
                x => x.Id,
                x => string.Join(" ", new[] { x.FirstName, x.LastName }.Where(y => !string.IsNullOrWhiteSpace(y))).Trim());

            var selectedLeaveType = string.IsNullOrWhiteSpace(leaveType) ? string.Empty : leaveType.Trim();
            var today = DateTime.Today;
            var effectiveStartDate = startDate;
            var effectiveEndDate = endDate;

            if (!effectiveStartDate.HasValue && !effectiveEndDate.HasValue)
            {
                effectiveStartDate = new DateTime(today.Year, 1, 1);
                effectiveEndDate = new DateTime(today.Year, 12, 31);
            }

            var filteredLeaves = leaves.AsEnumerable();

            if (employeeId.HasValue)
            {
                filteredLeaves = filteredLeaves.Where(x => x.EmployeeId == employeeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(selectedLeaveType))
            {
                filteredLeaves = filteredLeaves.Where(x => string.Equals(GetLeaveType(x), selectedLeaveType, StringComparison.OrdinalIgnoreCase));
            }

            if (effectiveStartDate.HasValue && effectiveEndDate.HasValue)
            {
                filteredLeaves = filteredLeaves.Where(x =>
                    x.EndDate.Date >= effectiveStartDate.Value.Date &&
                    x.StartDate.Date <= effectiveEndDate.Value.Date);
            }
            else if (effectiveStartDate.HasValue)
            {
                filteredLeaves = filteredLeaves.Where(x => x.EndDate.Date >= effectiveStartDate.Value.Date);
            }
            else if (effectiveEndDate.HasValue)
            {
                filteredLeaves = filteredLeaves.Where(x => x.StartDate.Date <= effectiveEndDate.Value.Date);
            }

            var reportItems = filteredLeaves
                .OrderByDescending(x => x.StartDate)
                .ThenByDescending(x => x.CreatedDate)
                .Select(x => new AdminLeaveReportItemViewModel
                {
                    Id = x.Id,
                    EmployeeName = employeeNames.TryGetValue(x.EmployeeId, out var employeeName) && !string.IsNullOrWhiteSpace(employeeName)
                        ? employeeName
                        : $"#{x.EmployeeId}",
                    LeaveType = GetLeaveType(x),
                    RequestedDays = GetRequestedDays(x),
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    Status = x.Status,
                    Reason = x.Reason,
                    CreatedDate = x.CreatedDate
                })
                .ToList();

            return new AdminLeaveReportViewModel
            {
                EmployeeId = employeeId,
                LeaveType = selectedLeaveType,
                StartDate = startDate,
                EndDate = endDate,
                TotalRecords = reportItems.Count,
                TotalDays = reportItems.Sum(x => x.RequestedDays),
                ApprovedCount = reportItems.Count(x => x.Status == 1),
                PendingCount = reportItems.Count(x => x.Status == 0),
                RejectedCount = reportItems.Count(x => x.Status != 0 && x.Status != 1),
                AppliedPeriodLabel = BuildPeriodLabel(effectiveStartDate, effectiveEndDate, !startDate.HasValue && !endDate.HasValue),
                EmployeeOptions = BuildEmployeeOptions(employees, employeeId),
                LeaveTypeOptions = BuildLeaveTypeOptions(
                    DefaultLeaveTypes
                        .Concat(leaves.Select(GetLeaveType))
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct()
                        .OrderBy(x => x),
                    selectedLeaveType),
                Reports = reportItems
            };
        }

        private static string GetStatusText(int status)
        {
            return status switch
            {
                0 => "Bekliyor",
                1 => "Onaylandı",
                _ => "Reddedildi"
            };
        }

        private static string EscapeCsv(string? value)
        {
            var normalized = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
            return $"\"{normalized.Replace("\"", "\"\"")}\"";
        }
    } 
}
