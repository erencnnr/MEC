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
    public class AssetService : IAssetService
    {
        private readonly IGenericRepository<Asset> _repository;

        public AssetService(IGenericRepository<Asset> repository)
        {
            _repository = repository;
        }

        public async Task<List<Asset>> GetAssetListAsync()
        {
            var assets = await _repository.GetAllAsync(x => x.School, x => x.AssetType, x => x.AssetStatus);
            return assets.ToList();
        }
    }
}
