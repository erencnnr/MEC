using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Portal.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MEC.Portal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IAnnouncementService _announcementService;

        // Hem Logger'ı hem de Duyuru servisimizi (IAnnouncementService) Constructor üzerinden alıyoruz
        public HomeController(ILogger<HomeController> logger, IAnnouncementService announcementService)
        {
            _logger = logger;
            _announcementService = announcementService;
        }

        // Veritabanı işlemi yapacağımız için metodu async (asenkron) hale getirdik
        public async Task<IActionResult> Index()
        {
            // Sadece durumu Aktif (IsActive = true) olan duyuruları getiriyoruz
            var activeAnnouncements = await _announcementService.GetActiveAnnouncementsAsync();

            // Veriyi (Duyuruları) View'a (Ekrana) gönderiyoruz
            return View(activeAnnouncements);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Tüm aktif duyuruların listeleneceği genel sayfa
        [HttpGet("/Announcements")]
        public async Task<IActionResult> Announcements()
        {
            var activeAnnouncements = await _announcementService.GetActiveAnnouncementsAsync();
            var sortedAnnouncements = activeAnnouncements.OrderByDescending(x => x.CreatedDate).ToList();
            return View(sortedAnnouncements);
        }
    }
}
