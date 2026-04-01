using MEC.Application.Abstractions.Service.LeaveService;
using MEC.Application.Abstractions.Service.LoggingService;
using MEC.Application.Abstractions.Service.LoggingService.Model;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Common;
using MEC.Domain.Entity.Employee;
using MEC.Portal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LeaveEntity = MEC.Domain.Entity.Leave.Leave;

namespace MEC.AssetManagementUI.Controllers
{
    public class AdminController : Controller
    {
        private const string UpdateLeaveStatusMethodName = "UpdateLeaveStatus";

        private readonly ILeaveService _leaveService;
        private readonly IAnnouncementService _announcementService;
        private readonly IGenericRepository<Employee> _employeeRepository;
        private readonly IGenericRepository<EmployeePortal> _employeePortalRepository;
        private readonly IGenericRepository<LeaveEntity> _leaveRepository;
        private readonly IUserActionLogService _userActionLogService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ILeaveService leaveService,
            IAnnouncementService announcementService,
            IGenericRepository<Employee> employeeRepository,
            IGenericRepository<EmployeePortal> employeePortalRepository,
            IGenericRepository<LeaveEntity> leaveRepository,
            IUserActionLogService userActionLogService,
            ILogger<AdminController> logger)
        {
            _leaveService = leaveService;
            _announcementService = announcementService;
            _employeeRepository = employeeRepository;
            _employeePortalRepository = employeePortalRepository;
            _leaveRepository = leaveRepository;
            _userActionLogService = userActionLogService;
            _logger = logger;
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
            return RedirectToAction("Index", "Announcement");
        }

        public async Task<IActionResult> LeaveRequests()
        {
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

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateLeaveStatus(int id, int status)
        {
            var leave = await _leaveRepository.GetByIdAsync(id);
            var employee = leave != null ? await _employeeRepository.GetByIdAsync(leave.EmployeeId) : null;
            var employeeName = BuildEmployeeName(employee, leave?.EmployeeId);
            var currentUser = User.Identity?.Name ?? "anonymous";
            var targetStatus = GetLeaveStatusDisplayName(status);

            try
            {
                var result = await _leaveService.UpdateLeaveStatusAsync(id, status);

                if (result)
                {
                    await TryLogLeaveStatusChangeAsync(
                        level: "Information",
                        message: $"İzin durumu güncellendi. İzin Id: {id}, Çalışan: {employeeName}, İşlem Yapan: {currentUser}, Yeni Durum: {targetStatus}.");

                    return RedirectToAction("LeaveRequests");
                }

                await TryLogLeaveStatusChangeAsync(
                    level: "Warning",
                    message: $"İzin durumu güncellenemedi. İzin Id: {id}, Çalışan: {employeeName}, İşlem Yapan: {currentUser}, Hedef Durum: {targetStatus}.");

                return BadRequest("Durum güncellenemedi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İzin durumu güncellenirken beklenmeyen bir hata oluştu. LeaveId: {LeaveId}, Status: {Status}", id, status);

                await TryLogLeaveStatusChangeAsync(
                    level: "Error",
                    message: $"İzin durumu güncellenirken hata oluştu. İzin Id: {id}, Çalışan: {employeeName}, İşlem Yapan: {currentUser}, Hedef Durum: {targetStatus}, Hata: {ex.Message}.");

                return StatusCode(500, "Durum güncellenirken beklenmeyen bir hata oluştu.");
            }
        }

        private async Task TryLogLeaveStatusChangeAsync(string level, string message)
        {
            try
            {
                await _userActionLogService.LogAsync(new UserActionLogEntryModel
                {
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    MacAddress = null,
                    User = User.Identity?.Name ?? "anonymous",
                    Timestamp = DateTime.UtcNow,
                    Message = message,
                    Level = level,
                    MethodName = UpdateLeaveStatusMethodName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı aksiyon logu yazılamadı. Method: {MethodName}", UpdateLeaveStatusMethodName);
            }
        }

        private static string BuildEmployeeName(Employee? employee, int? employeeId)
        {
            if (employee != null)
            {
                var fullName = string.Join(" ", new[] { employee.FirstName, employee.LastName }
                    .Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    return fullName;
                }
            }

            return employeeId.HasValue ? $"#{employeeId.Value}" : "Bilinmiyor";
        }

        private static string GetLeaveType(LeaveEntity leave)
        {
            return leave.GetType().GetProperty("LeaveType")?.GetValue(leave)?.ToString() ?? string.Empty;
        }

        private static decimal GetRequestedDays(LeaveEntity leave)
        {
            if (leave.RequestedDays > 0)
            {
                return leave.RequestedDays;
            }

            return LeaveDurationCalculator.CalculateRequestedDays(leave.StartDate, leave.EndDate);
        }

        private static decimal GetRemainingLeaveDays(LeaveEntity leave)
        {
            return leave.RemainingLeaveDays;
        }

        private static string GetLeaveStatusDisplayName(int status)
        {
            return status switch
            {
                0 => "Onay Bekliyor",
                1 => "Onaylandı",
                2 => "Reddedildi",
                3 => "İptal",
                _ => $"Durum {status}"
            };
        }
    }
}
