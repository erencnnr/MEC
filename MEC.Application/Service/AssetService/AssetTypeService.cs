using MEC.Application.Abstractions.Service.AssetService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Asset;
using Microsoft.EntityFrameworkCore; // DbUpdateException için gerekli
using System;
using System.Collections.Generic;
using System.Linq;
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
            var list = await _repository.GetAllAsync();
            return list.ToList();
        }

        // --- YENİ EKLENEN METOTLAR ---

        public async Task<AssetType> GetAssetTypeByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task CreateAssetTypeAsync(AssetType assetType)
        {
            // assetType.CreatedDate = DateTime.Now; // İsterseniz ekleyebilirsiniz
            await _repository.AddAsync(assetType);
        }

        public async Task UpdateAssetTypeAsync(AssetType assetType)
        {
            _repository.Update(assetType);
        }

        public async Task<string> DeleteAssetTypeAsync(int id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity != null)
                {
                    _repository.Delete(entity);
                    return null; // Başarılı
                }
                return "Kayıt bulunamadı.";
            }
            catch (DbUpdateException)
            {
                return "Bu eşya türü kullanımda olduğu için silinemez!";
            }
            catch (Exception ex)
            {
                return "Hata oluştu: " + ex.Message;
            }
        }
    }
}
