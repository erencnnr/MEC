using MEC.Application.Abstractions.Service.ServiceHistoryService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Asset;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MEC.Application.Service.ServiceHistoryService
{
    public class ServiceHistoryService : IServiceHistoryService
    {
        private readonly IGenericRepository<ServiceHistory> _repository;

        public ServiceHistoryService(IGenericRepository<ServiceHistory> repository)
        {
            _repository = repository;
        }

        public async Task AddServiceHistoryAsync(ServiceHistory serviceHistory)
        {
            await _repository.AddAsync(serviceHistory);
        }

        public async Task<List<ServiceHistory>> GetServiceHistoriesByAssetIdAsync(int assetId, string? sortOrder)
        {
            var histories = await _repository.GetAllAsync(x => x.AssetId == assetId);
            return ApplySorting(histories, sortOrder).ToList();
        }

        private IEnumerable<ServiceHistory> ApplySorting(IEnumerable<ServiceHistory> query, string? sortOrder)
        {
            return sortOrder switch
            {
                "SendDate_Asc" => query.OrderBy(x => x.SendDate),
                "SendDate_Desc" => query.OrderByDescending(x => x.SendDate),
                "ReturnDate_Asc" => query.OrderBy(x => x.ReturnDate),
                "ReturnDate_Desc" => query.OrderByDescending(x => x.ReturnDate),
                "Description_Asc" => query.OrderBy(x => x.Description),
                "Description_Desc" => query.OrderByDescending(x => x.Description),
                "ServiceCompany_Asc" => query.OrderBy(x => x.ServiceCompany),
                "ServiceCompany_Desc" => query.OrderByDescending(x => x.ServiceCompany),
                _ => query.OrderByDescending(x => x.SendDate)
            };
        }
    }
}
