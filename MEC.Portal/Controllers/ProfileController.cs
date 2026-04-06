using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Common;
using MEC.Domain.Common.Enum;
using MEC.Domain.Entity.Employee;
using MEC.Domain.Entity.Leave;
using MEC.Portal.Models;
using Microsoft.AspNetCore.Mvc;

namespace MEC.Portal.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IEmployeePortalService _profileService;
        private readonly IGenericRepository<Employee> _employeeRepository;
        private readonly IGenericRepository<Leave> _leaveRepository;

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
            var userEmail = User.Identity?.Name;
            var profileData = await _profileService.GetProfileByEmailAsync(userEmail);

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
                var employeeLeaves = (await _leaveRepository.GetAllAsync(x => x.EmployeeId == employee.Id, x => x.LeaveType))
                    .OrderByDescending(x => x.CreatedDate)
                    .ThenByDescending(x => x.Id)
                    .ToList();

                var pendingLeaves = employeeLeaves.Where(x => x.Status == (int)LeaveStatus.Pending);
                var pendingAnnualLeaves = pendingLeaves.Where(IsAnnualLeave);

                profileViewModel.PendingAnnualLeaveCount = pendingAnnualLeaves.Count();
                profileViewModel.PendingAnnualLeaveDays = pendingAnnualLeaves.Sum(GetRequestedDays);
            }

            return View(profileViewModel);
        }

        private static bool IsAnnualLeave(Leave leave)
        {
            return string.Equals(leave.LeaveType?.Code, LeaveTypeCodes.Annual, StringComparison.OrdinalIgnoreCase);
        }

        private static decimal GetRequestedDays(Leave leave)
        {
            if (leave.RequestedDays > 0)
            {
                return leave.RequestedDays;
            }

            return LeaveDurationCalculator.CalculateRequestedDays(leave.StartDate, leave.EndDate);
        }
    }
}
