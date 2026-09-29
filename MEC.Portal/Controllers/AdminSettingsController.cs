using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MEC.Portal.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Settings")]
public sealed class AdminSettingsController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}
