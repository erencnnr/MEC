using Microsoft.AspNetCore.Mvc;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Application.Abstractions.Service.LeaveService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Employee;
using MEC.Portal.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MEC.AssetManagementUI.Controllers
{
    public class AdminController : Controller
    {
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
    } 
}
