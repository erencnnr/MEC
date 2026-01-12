using MEC.Application.Abstractions.Application;
using MEC.Application.Abstractions.Service.AssetService.Model;
using MEC.Domain.Common;
using MEC.Domain.Entity.Asset;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEC.Application.Abstractions.Service.AssetService
{
    public interface IAssetService : IApplicationService
    {
        Task AddAssetAsync(Asset asset);
        Task<Asset> GetAssetByIdAsync(int id);
        Task<List<Asset>> GetAssetListAsync(AssetFilterRequestModel request);
        Task<PagedResult<Asset>> GetPagedAssetListAsync(AssetFilterRequestModel model);
        Task UpdateAssetAsync(Asset asset);
    }
}
