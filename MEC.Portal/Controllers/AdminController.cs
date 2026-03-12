using Microsoft.AspNetCore.Mvc;
using MEC.Application.Abstractions.Service.LeaveService;
using System.Threading.Tasks;

namespace MEC.AssetManagementUI.Controllers
{
    public class AdminController : Controller
    {
        private readonly ILeaveService _leaveService;

        // Dependency Injection ile servisimizi içeri alıyoruz
        public AdminController(ILeaveService leaveService)
        {
            _leaveService = leaveService;
        }

        public IActionResult Index()
        {
            return View();
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

            // Çektiğimiz verileri ekrana (View'a) gönderiyoruz
            return View(leaves);
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
    } 
}