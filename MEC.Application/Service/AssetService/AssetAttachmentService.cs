using MEC.Application.Abstractions.Service.AssetService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Asset;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MEC.Application.Service.AssetService
{
    public class AssetAttachmentService : IAssetAttachmentService
    {
        private readonly IGenericRepository<AssetAttachment> _repository;

        public AssetAttachmentService(IGenericRepository<AssetAttachment> repository)
        {
            _repository = repository;
        }

        // 1. Tüm Ekleri Getir
        public async Task<List<AssetAttachment>> GetAllAttachmentsAsync()
        {
            var attachments = await _repository.GetAllAsync();
            return attachments.ToList();
        }

        // 2. Belirli Bir Demirbaşa Ait Ekleri Getir
        public async Task<List<AssetAttachment>> GetAttachmentsByAssetIdAsync(int assetId)
        {
            var attachments = await _repository.GetAllAsync(x => x.AssetId == assetId);
            return attachments.ToList();
        }

        // 3. Ekleme
        public async Task CreateAsync(AssetAttachment attachment)
        {
            await _repository.AddAsync(attachment);
        }

        // 4. Silme
        public async Task DeleteAsync(int id)
        {
            var attachment = await _repository.GetByIdAsync(id);
            if (attachment != null)
            {
                _repository.Delete(attachment);
            }
        }

        // 5. AssetID ve Dosya Adına (Path) Göre Bulma
        // (Controller'da silme işlemi yaparken ID'yi bulmak için kullanıyoruz)
        public async Task<AssetAttachment> GetAttachmentByAssetAndNameAsync(int assetId, string fileName)
        {
            // Path sütununda dosya adını tuttuğumuzu varsayıyoruz
            var attachments = await _repository.GetAllAsync(x => x.AssetId == assetId && x.Path == fileName);
            return attachments.FirstOrDefault();
        }

        // 6. Güncelleme
        public async Task UpdateAttachmentAsync(AssetAttachment attachment)
        {
            // Repository'nizde UpdateAsync varsa onu kullanın, yoksa senkron Update
            _repository.Update(attachment);
        }

        // Asenkron Update (Eğer GenericRepository'e eklediyseniz)
        /*
        public async Task UpdateAttachmentAsync(AssetAttachment attachment)
        {
            await _repository.UpdateAsync(attachment);
        }
        */
    }
}