using MEC.Application.Abstractions.Application;
using MEC.Domain.Entity.Asset;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MEC.Application.Abstractions.Service.AssetService
{
    public interface IAssetTypeService : IApplicationService
    {
        // Mevcut metot
        Task<List<AssetType>> GetAssetTypeListAsync();

        // --- YENİ EKLENECEK METOTLAR ---
        Task<AssetType> GetAssetTypeByIdAsync(int id);
        Task CreateAssetTypeAsync(AssetType assetType);
        Task UpdateAssetTypeAsync(AssetType assetType);

        // Silme işleminde hata mesajı dönebilmek için string dönüş tipi kullanıyoruz
        Task<string> DeleteAssetTypeAsync(int id);
    }
}