using Microsoft.AspNetCore.Mvc;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Domain.Entity.School;
using System.Threading.Tasks;

namespace MEC.Portal.Controllers
{
    // Not: Yetkilendirme (Admin kontrolü) kısmını sayfaları tamamladıktan sonra ekleyeceğiz.
    public class AnnouncementController : Controller
    {
        private readonly IAnnouncementService _announcementService;

        public AnnouncementController(IAnnouncementService announcementService)
        {
            _announcementService = announcementService;
        }

        // 1. Duyuruların Listelendiği Ana Yönetim Sayfası
        public async Task<IActionResult> Index()
        {
            var announcements = await _announcementService.GetAllAnnouncementsAsync();
            return View(announcements);
        }

        // 2. Yeni Duyuru Ekleme Sayfasını Açan Metot (GET)
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // 3. Yeni Duyuruyu Veritabanına Kaydeden Metot (POST)
        [HttpPost]
        public async Task<IActionResult> Create(Announcement announcement)
        {
            if (ModelState.IsValid)
            {
                await _announcementService.AddAnnouncementAsync(announcement);
                return RedirectToAction("Index"); // Kaydettikten sonra listeye geri dön
            }
            return View(announcement);
        }

        // 4. Duyuru Silme Metodu
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _announcementService.DeleteAnnouncementAsync(id);
            return RedirectToAction("Index");
        }
    }
}
