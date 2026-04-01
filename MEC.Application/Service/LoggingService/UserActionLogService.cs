using MEC.Application.Abstractions.Service.LoggingService;
using MEC.Application.Abstractions.Service.LoggingService.Model;
using MEC.DAL.Config.Contexts;
using MEC.Domain.Entity.Logging;

namespace MEC.Application.Service.LoggingService
{
    public class UserActionLogService : IUserActionLogService
    {
        private readonly ApplicationDbContext _context;

        public UserActionLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(UserActionLogEntryModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            if (string.IsNullOrWhiteSpace(model.MethodName))
            {
                throw new ArgumentException("Method name is required.", nameof(model));
            }

            var entity = new UserActionLog
            {
                IpAddress = string.IsNullOrWhiteSpace(model.IpAddress) ? "unknown" : model.IpAddress,
                MacAddress = model.MacAddress,
                User = string.IsNullOrWhiteSpace(model.User) ? "anonymous" : model.User,
                Timestamp = model.Timestamp == default ? DateTime.UtcNow : model.Timestamp,
                Message = model.Message ?? string.Empty,
                Level = string.IsNullOrWhiteSpace(model.Level) ? "Information" : model.Level,
                MethodName = model.MethodName
            };

            _context.UserActionLogs.Add(entity);
            await _context.SaveChangesAsync();
        }
    }
}
