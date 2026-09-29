using MEC.Application.Service.LeaveService;
using Microsoft.AspNetCore.Mvc;
namespace MEC.Portal.ViewComponents;
public class LeaveAccountingViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int leaveId)
    {
        var accounting = HttpContext.RequestServices.GetService<LeaveAccountingService>();
        if (accounting == null) return Content("");
        return View(await accounting.GetDecisionDetailsAsync(leaveId, User.Identity?.Name ?? ""));
    }
}
