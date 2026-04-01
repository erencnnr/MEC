using MEC.Application.Abstractions.Service.LoggingService.Model;

namespace MEC.Application.Abstractions.Service.LoggingService
{
    public interface IUserActionLogService
    {
        Task LogAsync(UserActionLogEntryModel model);
    }
}
