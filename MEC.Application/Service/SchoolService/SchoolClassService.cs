using MEC.Application.Abstractions.Service.SchoolService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.School;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MEC.Application.Service.SchoolService
{
    public class SchoolClassService : ISchoolClassService
    {
        private readonly IGenericRepository<SchoolClass> _repository;

        public SchoolClassService(IGenericRepository<SchoolClass> repository)
        {
            _repository = repository;
        }

        public async Task<List<SchoolClass>> GetSchoolClassListAsync()
        {
            var schoolClasses = await _repository.GetAllAsync();
            return schoolClasses.ToList();
        }

        // --- YENİ EKLENEN METOTLAR ---

        public async Task<List<SchoolClass>> GetSchoolClassesBySchoolIdAsync(int schoolId)
        {
            // Tüm sınıfları çekip SchoolId'ye göre filtreliyoruz
            var allClasses = await _repository.GetAllAsync();
            return allClasses.Where(x => x.SchoolId == schoolId).ToList();
        }

        public async Task<SchoolClass> GetSchoolClassByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task CreateSchoolClassAsync(SchoolClass schoolClass)
        {
            // Yeni kayıt ekleme
            await _repository.AddAsync(schoolClass);
        }

        public async Task UpdateSchoolClassAsync(SchoolClass schoolClass)
        {
            // Güncelleme
            _repository.Update(schoolClass);
        }

        public async Task DeleteSchoolClassAsync(int id)
        {
            // Silme
            var entity = await _repository.GetByIdAsync(id);
            if (entity != null)
            {
                _repository.Delete(entity);
            }
        }
    }
}
