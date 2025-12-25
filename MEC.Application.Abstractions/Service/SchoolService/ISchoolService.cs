using MEC.Application.Abstractions.Application;
using MEC.Domain.Entity.School;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MEC.Application.Abstractions.Service.SchoolService
{
    public interface ISchoolService : IApplicationService
    {
        Task<List<School>> GetSchoolListAsync();
        Task<School> GetSchoolByIdAsync(int id);
        Task CreateSchoolAsync(School school);
        Task UpdateSchoolAsync(School school);
        Task DeleteSchoolAsync(int id);
    }
}
