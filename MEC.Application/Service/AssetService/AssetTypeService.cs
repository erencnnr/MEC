using MEC.Application.Abstractions.Service.AssetService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Asset;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEC.Application.Service.AssetService
{
    public class AssetTypeService : IAssetTypeService
    {
        private readonly IGenericRepository<AssetType> _repository;

        public AssetTypeService(IGenericRepository<AssetType> repository)
        {
            _repository = repository;
        }

        public async Task<List<AssetType>> GetAssetTypeListAsync()
        {
            var assetTypes = await _repository.GetAllAsync();
            return assetTypes.ToList();
        }
    }
}
