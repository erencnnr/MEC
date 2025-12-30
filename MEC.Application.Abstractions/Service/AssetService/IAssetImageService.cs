using MEC.Application.Abstractions.Application;
using MEC.Domain.Entity.Asset;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MEC.Application.Abstractions.Service.AssetService
{
    public interface IAssetImageService : IApplicationService
    {
        Task<List<AssetImage>> GetAssetImageListAsync();
        Task<List<AssetImage>> GetImagesByAssetIdAsync(int assetId);
        Task CreateAsync(AssetImage assetImage);

        // --- BU SATIRI EKLEYİN ---
        Task DeleteAsync(int id);
        Task<AssetImage> GetImageByAssetAndNameAsync(int assetId, string fileName);
        Task UpdateAssetImageAsync(AssetImage assetImage);
    }
}
