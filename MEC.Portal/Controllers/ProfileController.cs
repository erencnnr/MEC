using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.Application.Abstractions.Service.EmployeeService.Model;
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

        [HttpGet("/Profile")]
        public async Task<IActionResult> Index()
        {
            var userEmail = User.Identity?.Name ?? string.Empty;
            var profileSummary = await _profileService.GetProfileSummaryByEmailAsync(userEmail);

            if (profileSummary.Profile == null)
            {
                ViewBag.Error = "Profil bilgileriniz bulunamadı.";
                return View(new ProfileViewModel());
            }

            return View(new ProfileViewModel
            {
                Profile = profileSummary.Profile,
                PendingAnnualLeaveCount = profileSummary.PendingAnnualLeaveCount,
                PendingAnnualLeaveDays = profileSummary.PendingAnnualLeaveDays
            });
        }

        [HttpGet("/Profile/Edit")]
        public async Task<IActionResult> Edit(bool required = false)
        {
            var userEmail = User.Identity?.Name ?? string.Empty;
            var profile = await _profileService.GetSelfProfileEditAsync(userEmail);
            if (profile == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var model = MapProfileEditViewModel(profile);
            model.RequireCompletion = required || await _profileService.RequiresProfileCompletionAsync(userEmail);
            EnsureChildInputs(model);
            return View(model);
        }

        [HttpPost("/Profile/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileEditViewModel model)
        {
            EnsureChildInputs(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userEmail = User.Identity?.Name ?? string.Empty;
            var result = await _profileService.UpdateSelfProfileAsync(userEmail, new PortalSelfEditModel
            {
                Id = model.Id,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Title = model.Title,
                HireDate = model.HireDate,
                BirthDate = model.BirthDate,
                PhoneNumber = model.PhoneNumber,
                AddressText = model.AddressText,
                MaritalStatus = model.MaritalStatus,
                EducationUniversity = model.EducationUniversity,
                EducationFaculty = model.EducationFaculty,
                EducationDepartment = model.EducationDepartment,
                Children = model.Children.Select(MapChildInputModel).ToList()
            });

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["ProfileSuccess"] = result.Message;
            if (model.RequireCompletion)
            {
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction(nameof(Index));
        }

        private static ProfileEditViewModel MapProfileEditViewModel(PortalSelfEditModel profile)
        {
            return new ProfileEditViewModel
            {
                Id = profile.Id,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Email = profile.Email,
                Title = profile.Title,
                HireDate = profile.HireDate,
                BirthDate = profile.BirthDate,
                PhoneNumber = profile.PhoneNumber,
                AddressText = profile.AddressText,
                MaritalStatus = profile.MaritalStatus,
                EducationUniversity = profile.EducationUniversity,
                EducationFaculty = profile.EducationFaculty,
                EducationDepartment = profile.EducationDepartment,
                Children = profile.Children.Select(MapChildInputViewModel).ToList()
            };
        }

        private static ProfileChildInputViewModel MapChildInputViewModel(PortalUserChildEditModel child)
        {
            return new ProfileChildInputViewModel
            {
                Gender = child.Gender,
                BirthDate = child.BirthDate,
                EducationStatus = child.EducationStatus
            };
        }

        private static PortalUserChildEditModel MapChildInputModel(ProfileChildInputViewModel child)
        {
            return new PortalUserChildEditModel
            {
                Gender = child.Gender,
                BirthDate = child.BirthDate,
                EducationStatus = child.EducationStatus
            };
        }

        private static void EnsureChildInputs(ProfileEditViewModel model)
        {
            model.Children ??= new List<ProfileChildInputViewModel>();
            model.Children = model.Children
                .Where(x => x != null)
                .Select(x => new ProfileChildInputViewModel
                {
                    Gender = x.Gender,
                    BirthDate = x.BirthDate,
                    EducationStatus = x.EducationStatus
                })
                .ToList();
        }
    }
}
