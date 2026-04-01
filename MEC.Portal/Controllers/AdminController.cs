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
        private const int LeaveRequestsPageSize = 10;

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

        [HttpGet("/Admin/LeaveRequests")]
        public async Task<IActionResult> LeaveRequests(int page = 1)
        {
            var currentPage = page < 1 ? 1 : page;
            var leaves = await _leaveService.GetAllLeavesAsync();
            var employees = await _employeeRepository.GetAllAsync(x => !x.IsDeleted);
            var employeeNames = employees.ToDictionary(
                x => x.Id,
                x => string.Join(" ", new[] { x.FirstName, x.LastName }.Where(y => !string.IsNullOrWhiteSpace(y))).Trim());

            var mappedItems = leaves
                .Select(x => MapLeaveRequestItem(x, employeeNames))
                .ToList();

            var totalCount = mappedItems.Count;
            var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)LeaveRequestsPageSize);
            currentPage = Math.Min(currentPage, totalPages);

            var model = new AdminLeaveRequestListViewModel
            {
                Items = mappedItems.Skip((currentPage - 1) * LeaveRequestsPageSize).Take(LeaveRequestsPageSize).ToList(),
                CurrentPage = currentPage,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = LeaveRequestsPageSize
            };

            return View(model);
        }

        [HttpGet("/Admin/LeaveRequests/{id:int}")]
        public async Task<IActionResult> LeaveRequestDetail(int id)
        {
            var leave = await _leaveRepository.GetByIdAsync(id);
            if (leave == null)
            {
                return RedirectToAction(nameof(LeaveRequests));
            }

            var employee = await _employeeRepository.GetByIdAsync(leave.EmployeeId);
            var model = new AdminLeaveRequestDetailViewModel
            {
                Item = MapLeaveRequestItem(
                    leave,
                    new Dictionary<int, string> { [leave.EmployeeId] = BuildEmployeeName(employee, leave.EmployeeId) })
            };

            return View(model);
        }

        [HttpGet("/Admin/LeaveReport")]
        public IActionResult LeaveReport()
        {
            return View();
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

                    return RedirectToAction(nameof(LeaveRequests));
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

        private static AdminLeaveRequestViewModel MapLeaveRequestItem(LeaveEntity leave, IReadOnlyDictionary<int, string> employeeNames)
        {
            var employeeName = employeeNames.TryGetValue(leave.EmployeeId, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : $"#{leave.EmployeeId}";

            return new AdminLeaveRequestViewModel
            {
                Id = leave.Id,
                EmployeeId = leave.EmployeeId,
                EmployeeName = employeeName,
                LeaveType = GetLeaveType(leave),
                RequestedDays = GetRequestedDays(leave),
                StartDate = leave.StartDate,
                EndDate = leave.EndDate,
                Reason = leave.Reason,
                Status = leave.Status,
                RemainingLeaveDays = GetRemainingLeaveDays(leave),
                CreatedDate = leave.CreatedDate,
                StatusLabel = GetLeaveStatusDisplayName(leave.Status),
                StatusTone = GetLeaveStatusTone(leave.Status),
                DecisionDisplay = "-",
                CanTakeAction = leave.Status == 0
            };
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

        private static string GetLeaveStatusTone(int status)
        {
            return status switch
            {
                1 => "approved",
                2 => "rejected",
                3 => "cancelled",
                _ => "pending"
            };
        }
    }
}
