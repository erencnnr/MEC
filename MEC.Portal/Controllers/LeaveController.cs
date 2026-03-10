using Microsoft.AspNetCore.Mvc;
using MEC.Portal.Models;
using Microsoft.AspNetCore.Authorization;
using MEC.Portal.Services;
using System.Threading.Tasks;

namespace MEC.Portal.Controllers
{
    [Authorize]
    public class LeaveController : Controller
    {
        private readonly IEmailService _emailService;

        // Servisi Controller'a dahil ediyoruz
        public LeaveController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult RequestLeave()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RequestLeave(LeaveRequestViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Sisteme giriş yapmış olan kullanıcının e-postasını veya adını alıyoruz
                string userName = User.Identity?.Name ?? "Bilinmeyen Personel";

                // Arka planda mail gönderme işlemini tetikliyoruz
                await _emailService.SendLeaveRequestEmailAsync(userName, model.StartDate, model.EndDate, model.Reason);

                // Başarı mesajı gönderip Ana Sayfaya yönlendiriyoruz
                TempData["SuccessMessage"] = "İzin talebiniz başarıyla yöneticiye mail olarak iletildi.";
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }
    }
}