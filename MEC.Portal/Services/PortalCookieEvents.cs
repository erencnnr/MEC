using System.Security.Claims;
using MEC.Application.Abstractions.Service.ApprovalWorkflowService;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace MEC.Portal.Services;

public sealed class PortalCookieEvents(IApprovalWorkflowService workflow) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var actor = await workflow.GetActorAsync(context.Principal?.Identity?.Name ?? string.Empty);
        if (actor == null)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        var roles = new List<string>();
        if (actor.IsAdministrator) roles.Add("Admin");
        if (actor.IsLocationManager) roles.Add("Manager");
        if (actor.IsFinalApprover) roles.Add("FinalApprover");

        var principal = context.Principal!;
        var currentRoles = principal.FindAll(ClaimTypes.Role).Select(x => x.Value).ToHashSet();
        if (currentRoles.SetEquals(roles)) return;

        var identity = new ClaimsIdentity(
            principal.Claims.Where(x => x.Type != ClaimTypes.Role),
            CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaims(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        context.ReplacePrincipal(new ClaimsPrincipal(identity));
        context.ShouldRenew = true;
    }
}
