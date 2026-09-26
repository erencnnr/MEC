using MEC.Application.Abstractions.Service.SchoolService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MEC.Portal.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Schools")]
public sealed class SchoolSettingsController(ISchoolManagementService schools) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index() => View(await schools.GetAsync());

    [HttpPost("{id:int}/Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignManager(int id, int? managerId)
    {
        if (!ModelState.IsValid) return BadRequest();
        var result = await schools.AssignManagerAsync(id, managerId);
        TempData["SchoolMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
