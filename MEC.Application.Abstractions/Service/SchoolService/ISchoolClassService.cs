using MEC.Application.Abstractions.Application;
using MEC.Domain.Entity.School;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MEC.Application.Abstractions.Service.SchoolService
{
    public interface ISchoolClassService : IApplicationService
    {
        // Mevcut metot
        Task<List<SchoolClass>> GetSchoolClassListAsync();

        // Controller'ın ihtiyaç duyduğu YENİ metotlar
        Task<List<SchoolClass>> GetSchoolClassesBySchoolIdAsync(int schoolId);
        Task<SchoolClass> GetSchoolClassByIdAsync(int id);
        Task CreateSchoolClassAsync(SchoolClass schoolClass);
        Task UpdateSchoolClassAsync(SchoolClass schoolClass);
        Task DeleteSchoolClassAsync(int id);
    }
}
