using Microsoft.AspNetCore.Mvc;

namespace MEC.AssetManagementUI.Controllers
{
    public class SettingsController : Controller
    {
        public IActionResult Index()
        {
            return View("Settings");
        }
    }
}
