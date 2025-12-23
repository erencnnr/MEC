using MEC.Application.Abstractions.Service.AssetService;
using MEC.Application.Abstractions.Service.AssetService.Model;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Asset;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MEC.Application.Service.AssetService
{
    public class AssetService : IAssetService
    {
        private readonly IGenericRepository<Asset> _repository;

        public AssetService(IGenericRepository<Asset> repository)
        {
            _repository = repository;
        }

        public async Task<List<Asset>> GetAssetListAsync(AssetFilterRequestModel request)
        {
            Expression<Func<Asset, bool>> predicate = x =>
                (string.IsNullOrEmpty(request.SearchText) || x.Description.Contains(request.SearchText) || x.Name.Contains(request.SearchText)) &&
                (!request.SchoolId.HasValue || x.SchoolId == request.SchoolId) &&
                (!request.AssetTypeId.HasValue || x.AssetTypeId == request.AssetTypeId) &&
                (!request.AssetStatusId.HasValue || x.AssetStatusId == request.AssetStatusId);

            var assets = await _repository.GetAllAsync(predicate,x => x.School,x => x.AssetType,x => x.AssetStatus);
            return assets.ToList();
        }
    }
}
