using MEC.Application.Abstractions.Service.LoggingService;
using MEC.Application.Abstractions.Service.LoggingService.Model;
using MEC.DAL.Config.Contexts;
using MEC.Domain.Entity.Logging;

namespace MEC.Application.Service.LoggingService
{
    public class ApiLogService : IApiLogService
    {
        private readonly ApplicationDbContext _context;

        public ApiLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(ApiLogEntryModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            if (string.IsNullOrWhiteSpace(model.MethodName))
            {
                throw new ArgumentException("Method name is required.", nameof(model));
            }

            var entity = new ApiLog
            {
                IpAddress = string.IsNullOrWhiteSpace(model.IpAddress) ? "unknown" : model.IpAddress,
                MacAddress = model.MacAddress,
                User = string.IsNullOrWhiteSpace(model.User) ? "anonymous" : model.User,
                Timestamp = model.Timestamp == default ? DateTime.UtcNow : model.Timestamp,
                Message = model.Message ?? string.Empty,
                Level = string.IsNullOrWhiteSpace(model.Level) ? "Information" : model.Level,
                MethodName = model.MethodName,
                RequestPath = model.RequestPath,
                HttpMethod = model.HttpMethod,
                StatusCode = model.StatusCode,
                RequestBody = model.RequestBody,
                ResponseBody = model.ResponseBody,
                QueryString = model.QueryString
            };

            _context.ApiLogs.Add(entity);
            await _context.SaveChangesAsync();
        }
    }
}
