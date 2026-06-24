using MEC.Application.Abstractions.Service.EmployeeService;

namespace MEC.Portal.Middleware
{
    public class ProfileCompletionMiddleware
    {
        private readonly RequestDelegate _next;

        public ProfileCompletionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IEmployeePortalService employeePortalService)
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                await _next(context);
                return;
            }

            if (IsAllowedPath(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var email = context.User.Identity?.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(email))
            {
                await _next(context);
                return;
            }

            if (await employeePortalService.RequiresProfileCompletionAsync(email))
            {
                context.Response.Redirect("/Profile/Edit?required=true");
                return;
            }

            await _next(context);
        }

        private static bool IsAllowedPath(PathString path)
        {
            return path.StartsWithSegments("/Profile/Edit")
                || path.StartsWithSegments("/Account/Logout")
                || path.StartsWithSegments("/Account/AccessDenied")
                || path.StartsWithSegments("/Home/Error");
        }
    }
}
