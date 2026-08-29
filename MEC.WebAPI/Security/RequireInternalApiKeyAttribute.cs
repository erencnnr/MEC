using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Cryptography;
using System.Text;

namespace MEC.WebAPI.Security
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RequireInternalApiKeyAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public const string HeaderName = "X-MEC-Internal-Key";

        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var expectedApiKey = configuration["InternalApi:ApiKey"]?.Trim();

            if (string.IsNullOrWhiteSpace(expectedApiKey))
            {
                context.Result = new ObjectResult(new
                {
                    success = false,
                    message = "Dosya işlemleri için servis güvenliği yapılandırılmamış."
                })
                {
                    StatusCode = StatusCodes.Status503ServiceUnavailable
                };

                return Task.CompletedTask;
            }

            var providedApiKey = context.HttpContext.Request.Headers[HeaderName].ToString().Trim();
            if (!ApiKeysEqual(expectedApiKey, providedApiKey))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "Bu dosya işlemi için yetkiniz bulunmuyor."
                });
            }

            return Task.CompletedTask;
        }

        private static bool ApiKeysEqual(string expectedApiKey, string providedApiKey)
        {
            var expectedBytes = Encoding.UTF8.GetBytes(expectedApiKey);
            var providedBytes = Encoding.UTF8.GetBytes(providedApiKey);

            return expectedBytes.Length == providedBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
        }
    }
}
