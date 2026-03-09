using MEC.Application.Abstractions.Service.EmployeeService;
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

            return View(profileData);
        }
    }
}