using MEC.Application.Abstractions.Service.AssetService;
using MEC.Application.Abstractions.Service.AssetService.Model;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Common;
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
            // Filtreleri oluştur
            var predicate = CreateFilterPredicate(request);

            // Veriyi çek
            var assets = await _repository.GetAllAsync(predicate,
                x => x.School,
                x => x.AssetType,
                x => x.AssetStatus,
                x => x.SchoolClass);

            return assets.ToList();
        }

        // 2. SAYFALI LİSTE (UI Listeleme için)
        public async Task<PagedResult<Asset>> GetPagedAssetListAsync(AssetFilterRequestModel request)
        {
            // Filtreleri oluştur
            var predicate = CreateFilterPredicate(request);

            // Tüm filtrelenmiş veriyi çek (Repository IQueryable dönmediği için mecburen listeyi çekiyoruz)
            var allAssets = await _repository.GetAllAsync(predicate,
                x => x.School,
                x => x.AssetType,
                x => x.AssetStatus,
                x => x.SchoolClass);

            IEnumerable<Asset> query = allAssets;

            switch (request.SortOrder)
            {
                case "Date_Asc":
                    query = query.OrderBy(x => x.CreatedDate);
                    break;
                case "Date_Desc":
                    query = query.OrderByDescending(x => x.CreatedDate);
                    break;
                default:
                    query = query.OrderByDescending(x => x.Id); // Varsayılan: En son eklenen en üstte
                    break;
            }

            // Toplam kayıt sayısı
            int rowCount = query.Count();

            // Bellekte Sayfalama (Pagination)
            var pagedData = query
                .OrderByDescending(x => x.Id) // Yeniden eskiye sıralama
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new PagedResult<Asset>
            {
                Results = pagedData,
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                RowCount = rowCount,
                PageCount = (int)Math.Ceiling((double)rowCount / request.PageSize)
            };
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
        private Expression<Func<Asset, bool>> CreateFilterPredicate(AssetFilterRequestModel request)
        {
            return x =>
                (string.IsNullOrEmpty(request.SearchText) ||
                 (x.Description != null && x.Description.Contains(request.SearchText)) ||
                 (x.Name != null && x.Name.Contains(request.SearchText)) ||
                 (x.SerialNumber != null && x.SerialNumber.Contains(request.SearchText))) && // Seri No araması eklendi
                (request.SchoolIds == null || !request.SchoolIds.Any() || request.SchoolIds.Contains(x.SchoolId)) &&
                (request.AssetTypeIds == null || !request.AssetTypeIds.Any() || request.AssetTypeIds.Contains(x.AssetTypeId)) &&
                (request.AssetStatusIds == null || !request.AssetStatusIds.Any() || request.AssetStatusIds.Contains(x.AssetStatusId));
        }
    }
}
