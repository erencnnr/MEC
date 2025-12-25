using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Employee;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MEC.Application.Service.EmployeeService
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IGenericRepository<Employee> _repository;
        private readonly IGenericRepository<EmployeeType> _typeRepository;

        public EmployeeService(IGenericRepository<Employee> repository, IGenericRepository<EmployeeType> typeRepository)
        {
            _repository = repository;
            _typeRepository = typeRepository;
        }

        public async Task<List<Employee>> GetEmployeeListAsync()
        {
            var employees = await _repository.GetAllAsync();
            return employees.ToList();
        }

        // Yeni eklenen metot
        public async Task CreateEmployeeAsync(Employee employee)
        {

            employee.CreatedDate = DateTime.Now;
            // Burada iş kuralları (validasyon vb.) olabilir.

            await _repository.AddAsync(employee);
        }

        public async Task<List<EmployeeType>> GetEmployeeTypesAsync()
        {
            var types = await _typeRepository.GetAllAsync();
            return types.ToList();
        }

        // YENİ EKLENEN METOT İMPLEMENTASYONU:
        public async Task DeleteEmployeeAsync(int id)
        {
            var employee = await _repository.GetByIdAsync(id);
            if (employee != null)
            {
                employee.IsDeleted = true; // Pasife çekiyoruz
                                           // UpdateDate'i de güncelleyelim
                employee.UpdateDate = DateTime.Now;

                _repository.Update(employee); // GenericRepository'de SaveChanges olduğu için DB'ye yansır.
            }
        }
    }
}