using MEC.Application.Abstractions.Application;
using MEC.Domain.Entity.Asset;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MEC.Application.Abstractions.Service.ServiceHistoryService
{
    public interface IServiceHistoryService : IApplicationService
    {
        Task AddServiceHistoryAsync(ServiceHistory serviceHistory);
        Task<List<ServiceHistory>> GetServiceHistoriesByAssetIdAsync(int assetId, string? sortOrder);
    }
}
