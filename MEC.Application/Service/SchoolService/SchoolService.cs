using MEC.Application.Abstractions.Service.SchoolService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.School;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MEC.Application.Service.SchoolService
{
    public class SchoolService : ISchoolService
    {
        private readonly IGenericRepository<School> _repository;

        public SchoolService(IGenericRepository<School> repository)
        {
            _repository = repository;
        }

        public async Task<List<School>> GetSchoolListAsync()
        {
            var schools = await _repository.GetAllAsync();
            return schools.ToList();
        }

        public async Task<School> GetSchoolByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task CreateSchoolAsync(School school)
        {
            // İsterseniz burada CreateDate ataması yapabilirsiniz
            // school.CreateDate = DateTime.Now; 
            await _repository.AddAsync(school);
        }

        public async Task UpdateSchoolAsync(School school)
        {
            _repository.Update(school);
        }

        public async Task DeleteSchoolAsync(int id)
        {
            var school = await _repository.GetByIdAsync(id);
            if (school != null)
            {
                _repository.Delete(school);
            }
        }
    }
}
