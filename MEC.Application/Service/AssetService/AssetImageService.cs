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
    public class AssetImageService : IAssetImageService
    {
        private readonly IGenericRepository<AssetImage> _repository;

        public AssetImageService(IGenericRepository<AssetImage> repository)
        {
            _repository = repository;
        }

        public async Task<List<AssetImage>> GetAssetImageListAsync()
        {
            var assetImages = await _repository.GetAllAsync();
            return assetImages.ToList();
        }
    }
}
