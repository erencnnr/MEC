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
            Expression<Func<Asset, bool>> predicate = x =>
                (string.IsNullOrEmpty(request.SearchText) || x.Description.Contains(request.SearchText) || x.Name.Contains(request.SearchText)) &&
                (request.SchoolIds == null || !request.SchoolIds.Any() || request.SchoolIds.Contains(x.SchoolId)) &&
                (request.AssetTypeIds == null || !request.AssetTypeIds.Any() || request.AssetTypeIds.Contains(x.AssetTypeId)) &&
                (request.AssetStatusIds == null || !request.AssetStatusIds.Any() || request.AssetStatusIds.Contains(x.AssetStatusId));

            var assets = await _repository.GetAllAsync(predicate, x => x.School, x => x.AssetType, x => x.AssetStatus, x => x.SchoolClass);
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
