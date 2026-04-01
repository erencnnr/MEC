using MEC.Application.Abstractions.Service.LoggingService.Model;

namespace MEC.Application.Abstractions.Service.LoggingService
{
    public interface IApiLogService
    {
        Task LogAsync(ApiLogEntryModel model);
    }
}
