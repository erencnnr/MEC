using MEC.Application.Abstractions.Service.AssetService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Asset;
using Microsoft.EntityFrameworkCore; // DbUpdateException için şart
using System;
using System.Collections.Generic;
using System.Linq;
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
            var list = await _repository.GetAllAsync();
            return list.ToList();
        }

        // --- YENİ EKLENEN METOTLAR ---

        public async Task<AssetStatus> GetAssetStatusByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task CreateAssetStatusAsync(AssetStatus assetStatus)
        {
            await _repository.AddAsync(assetStatus);
        }

        public async Task UpdateAssetStatusAsync(AssetStatus assetStatus)
        {
            _repository.Update(assetStatus);
        }

        public async Task<string> DeleteAssetStatusAsync(int id)
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
                return "Bu eşya durumu şu anda kullanımda olduğu için silinemez!";
            }
            catch (Exception ex)
            {
                return "Hata oluştu: " + ex.Message;
            }
        }
    }
}