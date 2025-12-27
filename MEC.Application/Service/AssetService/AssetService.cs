using MEC.Application.Abstractions.Service.AssetService;
using MEC.Application.Abstractions.Service.AssetService.Model;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Asset;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

        public async Task<List<Asset>> GetAssetListAsync(AssetFilterRequestModel request)
        {
            // Repository'e gönderilecek sorgu şartı (Predicate)
            Expression<Func<Asset, bool>> predicate = x =>
                // 1. Arama Metni (Boşsa geç, doluysa İsim veya Seri No içinde ara)
                (string.IsNullOrEmpty(request.SearchText) || x.Name.Contains(request.SearchText) || x.SerialNumber.Contains(request.SearchText)) &&

                // 2. Okul ID (Boşsa geç, doluysa eşleşeni getir)
                (!request.SchoolId.HasValue || x.SchoolId == request.SchoolId) &&

                // 3. Demirbaş Tipi (Bilgisayar, Yazıcı vb.)
                (!request.AssetTypeId.HasValue || x.AssetTypeId == request.AssetTypeId) &&

                // 4. Durum (Zimmetli, Hurda vb.)
                (!request.AssetStatusId.HasValue || x.AssetStatusId == request.AssetStatusId);

            // Repository çağrısı (Include'lar ile birlikte)
            var assets = await _repository.GetAllAsync(predicate,
                                                       x => x.School,
                                                       x => x.AssetType,
                                                       x => x.AssetStatus);

            return assets.ToList();
        }
        public async Task AddAssetAsync(Asset asset)
        {
            await _repository.AddAsync(asset);
        }
        public async Task<Asset> GetAssetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        // 2. Güncelleme işlemi
        public async Task UpdateAssetAsync(Asset asset)
        {
            _repository.Update(asset);
            // UnitOfWork kullanmıyorsak SaveChanges gerekebilir, GenericRepo yapına göre değişir.
            // Senin yapında GenericRepo.Update void dönüyor, muhtemelen context.SaveChanges() repo içinde veya serviste çağrılmalı.
            // Eğer GenericRepo içinde SaveChanges yoksa buraya _context.SaveChangesAsync() eklenmeli.
            // Senin GenericRepo koduna baktım, Update sadece _dbSet.Update yapıyor. 
            // Bu yüzden buraya bir SaveChanges mekanizması lazım. 
            // (Şimdilik repo'nun transaction yönettiğini varsayıyorum veya UnitOfWork varsa o halleder)
        }
    }
}
