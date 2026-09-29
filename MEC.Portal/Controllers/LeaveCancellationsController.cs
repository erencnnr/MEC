using MEC.Application.Service.LeaveService;
using MEC.Application.Abstractions.Service.ApprovalWorkflowService;
using MEC.Application.Abstractions.Service.NotificationService;
using MEC.Application.Abstractions.Service.NotificationService.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using MEC.Portal.Models;

namespace MEC.Portal.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
public class LeaveCancellationsController(LeaveAccountingService accounting, IApprovalWorkflowService workflow,
    IWorkflowNotificationService notifications, ILogger<LeaveCancellationsController> logger) : Controller
{
    [Authorize(Roles = "Admin,FinalApprover")]
    [HttpGet("/Admin/LeaveCancellations")]
    public async Task<IActionResult> Index([FromQuery] CancellationFilter filter)
    {
        try
        {
            if (!ModelState.IsValid) throw new InvalidOperationException("Filtre tarihlerini kontrol edin.");
            return View(await accounting.GetCancellationsAsync(User.Identity!.Name!, filter));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(new CancellationListPage { Filter = filter });
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [Authorize(Roles = "Admin,FinalApprover")]
    [HttpGet("/Admin/LeaveCancellations/{id:int}")]
    public async Task<IActionResult> Detail(int id, string? returnUrl)
    {
        try
        {
            var item = await accounting.GetCancellationAsync(id, User.Identity!.Name!);
            return item == null ? NotFound() : View(new CancellationDetailViewModel
            { Item = item, ReturnUrl = ListReturnUrl(returnUrl) });
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    private string ListReturnUrl(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) && (returnUrl == "/Admin/LeaveCancellations" ||
            returnUrl!.StartsWith("/Admin/LeaveCancellations?", StringComparison.Ordinal))
        ? returnUrl! : "/Admin/LeaveCancellations";

    [HttpPost("/Leave/History/{id:int}/RequestCancellation")]
    public async Task<IActionResult> RequestCancellation(int id, string reason)
    {
        try
        {
            if (!ModelState.IsValid) throw new InvalidOperationException("İptal gerekçesini kontrol edin.");
            await accounting.RequestCancellationAsync(id, reason, User.Identity!.Name!);
            TempData["AccountingMessage"] = "İptal talebi Genel Müdürlük/Admin onayına gönderildi.";
        }
        catch (InvalidOperationException ex) { TempData["AccountingMessage"] = ex.Message; }
        return Redirect($"/Leave/History/{id}");
    }
    [Authorize(Roles = "Admin,FinalApprover")]
    [HttpPost("/Admin/LeaveCancellations/{id:int}/Decision")]
    public async Task<IActionResult> Decide(int id, bool approve, string? returnUrl)
    {
        try
        {
            if (!ModelState.IsValid) throw new InvalidOperationException("Geçersiz karar.");
            var actor = User.Identity!.Name!;
            var changed = await accounting.DecideCancellationAsync(id, approve, actor);
            if (changed)
            {
                try
                {
                    var detail = await accounting.GetDecisionDetailsAsync(id, actor);
                    var route = await workflow.ResolveRouteAsync(detail.Leave.EmployeeId);
                    if (route != null)
                        await notifications.NotifyLeaveRequestCancelledAsync(new LeaveRequestCancelledNotificationModel
                        {
                            LeaveId = id, EmployeeName = route.EmployeeName, StartDate = detail.Leave.StartDate,
                            EndDate = detail.Leave.EndDate, Reason = detail.Cancellation?.Reason ?? "", CancelledBy = actor,
                            Approvers = route.ManagerApprovers.Append(route.FinalApprover).GroupBy(x => x.Email, StringComparer.OrdinalIgnoreCase)
                                .Select(x => new WorkflowNotificationRecipientModel { Email = x.Key, DisplayName = x.First().DisplayName }).ToList(),
                            TriggeredByUser = actor, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
                        });
                }
                catch (Exception ex) { logger.LogError(ex, "İptal kaydedildi fakat bildirim gönderilemedi: {LeaveId}", id); }
            }
            TempData["AccountingMessage"] = "İptal talebinin durumu güncellendi.";
        }
        catch (InvalidOperationException ex) { TempData["AccountingMessage"] = ex.Message; }
        return Redirect(QueryHelpers.AddQueryString($"/Admin/LeaveCancellations/{id}", "returnUrl", ListReturnUrl(returnUrl)));
    }
}
