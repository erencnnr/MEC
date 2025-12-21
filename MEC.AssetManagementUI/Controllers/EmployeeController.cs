using Microsoft.AspNetCore.Mvc;

namespace MEC.AssetManagementUI.Controllers
{
    public class EmployeeController : Controller
    {
        public IActionResult Index()
        {
            return View("EmployeeList");
        }
    }
}
