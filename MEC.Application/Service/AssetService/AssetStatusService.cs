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
    public class AssetStatusService : IAssetStatusService
    {
        private readonly IGenericRepository<AssetStatus> _repository;

        public AssetStatusService(IGenericRepository<AssetStatus> repository)
        {
            _repository = repository;
        }

        public async Task<List<AssetStatus>> GetAssetStatusListAsync()
        {
            var assetStatus = await _repository.GetAllAsync();
            return assetStatus.ToList();
        }
    }
}
