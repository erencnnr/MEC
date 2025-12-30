using MEC.Application.Abstractions.Service.AssetService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Asset;
using System.Collections.Generic;
using System.Linq;
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

        // Eğer GenericRepository'niz otomatik olarak IsDeleted kontrolü yapmıyorsa bu kod doğrudur:
        public async Task<List<AssetImage>> GetImagesByAssetIdAsync(int assetId)
        {
            // Burada IsDeleted kontrolü OLMAMALI
            var images = await _repository.GetAllAsync(x => x.AssetId == assetId);
            return images.ToList();
        }

        // --- HATA ÇÖZÜMÜ: EKSİK OLAN METOT BURADA ---
        public async Task CreateAsync(AssetImage assetImage)
        {
            await _repository.AddAsync(assetImage);
        }
        public async Task DeleteAsync(int id)
        {
            var image = await _repository.GetByIdAsync(id);
            if (image != null)
            {
                _repository.Delete(image);
            }
        }
        public async Task<AssetImage> GetImageByAssetAndNameAsync(int assetId, string fileName)
        {
            // AssetId ve FileName eşleşen ilk kaydı getirir.
            var images = await _repository.GetAllAsync(x => x.AssetId == assetId && x.Path == fileName);
            return images.FirstOrDefault();
        }
        public async Task UpdateAssetImageAsync(AssetImage assetImage)
        {
            _repository.Update(assetImage);
        }
    }
}
