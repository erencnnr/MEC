using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.Portal.Models;
using Microsoft.AspNetCore.Mvc;

namespace MEC.Portal.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IEmployeePortalService _profileService;

        public ProfileController(IEmployeePortalService profileService)
        {
            _profileService = profileService;
        }

        public async Task<IActionResult> Index()
        {
            var userEmail = User.Identity?.Name ?? string.Empty;
            var profileSummary = await _profileService.GetProfileSummaryByEmailAsync(userEmail);

            if (profileSummary.Profile == null)
            {
                ViewBag.Error = "Profil bilgileriniz bulunamadı.";
                return View();
            }

            return View(new ProfileViewModel
            {
                Profile = profileSummary.Profile,
                PendingAnnualLeaveCount = profileSummary.PendingAnnualLeaveCount,
                PendingAnnualLeaveDays = profileSummary.PendingAnnualLeaveDays
            });
        }
    }
}
