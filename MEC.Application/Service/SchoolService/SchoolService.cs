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
    public class SchoolService : ISchoolService
    {
        private readonly IGenericRepository<School> _repository;
        public SchoolService(IGenericRepository<School> repository)
        {
            _repository = repository;
        }
        public async Task<List<School>> GetSchoolListAsync()
        {
            // Generic Repository'deki GetAllAsync metodunu kullanıyoruz
            var schools = await _repository.GetAllAsync();

            // IEnumerable dönen veriyi List'e çevirip döndürüyoruz
            return schools.ToList();
        }
    }
}
