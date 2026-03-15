using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Employee;
using MEC.Domain.Entity.Leave;
using MEC.Portal.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace MEC.Portal.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IEmployeePortalService _profileService;
        private readonly IGenericRepository<Employee> _employeeRepository;
        private readonly IGenericRepository<Leave> _leaveRepository;
        private const int PendingStatus = 0;
        private const string AnnualLeaveType = "Yıllık İzin";

        public ProfileController(
            IEmployeePortalService profileService,
            IGenericRepository<Employee> employeeRepository,
            IGenericRepository<Leave> leaveRepository)
        {
            _profileService = profileService;
            _employeeRepository = employeeRepository;
            _leaveRepository = leaveRepository;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Sisteme giriş yapmış kullanıcının email adresini alıyoruz (AccountController'da claim olarak kaydetmiştik)
            var userEmail = User.Identity.Name;

            // 2. Bu email'e ait verileri veritabanından çekiyoruz
            var profileData = await _profileService.GetProfileByEmailAsync(userEmail);

            // 3. Eğer kullanıcı DB'de yoksa hata veya boş model dönebiliriz
            if (profileData == null)
            {
                ViewBag.Error = "Profil bilgileriniz bulunamadı.";
                return View();
            }

            var profileViewModel = new ProfileViewModel
            {
                Profile = profileData
            };

            var employee = (await _employeeRepository.GetAllAsync(x => x.Email == userEmail && !x.IsDeleted)).FirstOrDefault();
            if (employee != null)
            {
                var employeeLeaves = (await _leaveRepository.GetAllAsync(x => x.EmployeeId == employee.Id))
                    .OrderByDescending(x => x.CreatedDate)
                    .ThenByDescending(x => x.Id)
                    .ToList();

                var pendingLeaves = employeeLeaves.Where(x => x.Status == PendingStatus);

                var pendingAnnualLeaves = pendingLeaves.Where(IsAnnualLeave);

                profileViewModel.PendingAnnualLeaveCount = pendingAnnualLeaves.Count();
                profileViewModel.PendingAnnualLeaveDays = pendingAnnualLeaves.Sum(GetRequestedDays);
                profileViewModel.LeaveHistory = employeeLeaves.Select(x => new LeaveHistoryItemViewModel
                {
                    Id = x.Id,
                    LeaveType = GetLeaveType(x),
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    RequestedDays = GetRequestedDays(x),
                    Reason = x.Reason,
                    Status = x.Status,
                    RemainingLeaveDays = GetRemainingLeaveDays(x),
                    CreatedDate = x.CreatedDate
                }).ToList();
            }

            return View(profileViewModel);
        }

        private static bool IsAnnualLeave(Leave leave)
        {
            return string.Equals(GetLeaveType(leave), AnnualLeaveType, StringComparison.Ordinal);
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
    }
}
