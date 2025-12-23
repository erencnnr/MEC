using MEC.Application.Abstractions.Service.SchoolService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.School;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    }
}
