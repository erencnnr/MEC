using MEC.Application.Abstractions.Application;
using MEC.Domain.Entity.Asset;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MEC.Application.Abstractions.Service.AssetService
{
    public interface IAssetStatusService : IApplicationService
    {
        // Mevcut metot
        Task<List<AssetStatus>> GetAssetStatusListAsync();

        // --- YENİ EKLENECEK METOTLAR ---
        Task<AssetStatus> GetAssetStatusByIdAsync(int id);
        Task CreateAssetStatusAsync(AssetStatus assetStatus);
        Task UpdateAssetStatusAsync(AssetStatus assetStatus);

        // Silme işleminde hata varsa mesaj döner, yoksa null
        Task<string> DeleteAssetStatusAsync(int id);
    }
}
