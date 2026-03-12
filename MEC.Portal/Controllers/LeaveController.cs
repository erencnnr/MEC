using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;

// Sizin projenizdeki yollar
using MEC.Portal.Models;
using MEC.Portal.Services;
using MEC.DAL.Config.Contexts; // ApplicationDbContext için gerekli
using MEC.Domain.Entity.Employee; // Leave ve Employee entity'leri için gerekli

namespace MEC.Portal.Controllers
{
    [Authorize]
    public class LeaveController : Controller
    {
        private readonly IEmailService _emailService;

        // --- 1. HATA ÇÖZÜMÜ: _context burada tanımlanıyor ---
        private readonly ApplicationDbContext _context;

        // Yapıcı metot (Constructor) ile hem mail servisini hem db context'i alıyoruz
        public LeaveController(IEmailService emailService, ApplicationDbContext context)
        {
            _emailService = emailService;
            _context = context;
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
                // Sisteme giriş yapmış olan kullanıcının e-postasını alıyoruz
                string userEmail = User.Identity?.Name;

                // Veritabanından bu e-posta adresine sahip olan personeli buluyoruz
                var currentEmployee = _context.Employees.FirstOrDefault(e => e.Email == userEmail);

                if (currentEmployee == null)
                {
                    ModelState.AddModelError("", "İşlem başarısız: Sisteme giriş yapan personel kaydı veritabanında bulunamadı.");
                    return View(model);
                }

                // Yeni izin kaydını oluşturuyoruz
                var newLeave = new Leave
                {
                    EmployeeId = currentEmployee.Id,
                    StartDate = Convert.ToDateTime(model.StartDate),
                    EndDate = Convert.ToDateTime(model.EndDate),
                    Reason = model.Reason,
                    CreatedDate = DateTime.Now
                };

                // Veritabanına Ekle ve Kaydet
                _context.Leaves.Add(newLeave);
                await _context.SaveChangesAsync();

                // -----------------------------------------------------------------
                // ŞİMDİLİK MAİL GÖNDERME İŞLEMİNİ ASKIYA ALDIK (YORUM SATIRI YAPTIK)
                // İleride şirket SMTP ayarlarını yaptığımızda burayı tekrar açacağız.
                // -----------------------------------------------------------------
                // string fullName = $"{currentEmployee.FirstName} {currentEmployee.LastName}";
                // await _emailService.SendLeaveRequestEmailAsync(fullName, model.StartDate, model.EndDate, model.Reason);


                // İsmi sadece bu sayfaya özel olacak şekilde değiştirdik
                TempData["LeaveSuccess"] = "İzin talebiniz başarıyla alınmıştır.";
                return RedirectToAction("RequestLeave");
            }

            return View(model);
        }
    }
}