using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Employee;
using System;
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

        // --- Employee Metotları ---
        public async Task<List<Employee>> GetEmployeeListAsync()
        {
            var employees = await _repository.GetAllAsync();
            return employees.ToList();
        }

        public async Task CreateEmployeeAsync(Employee employee)
        {
            employee.CreatedDate = DateTime.Now;
            await _repository.AddAsync(employee);
        }

        public async Task DeleteEmployeeAsync(int id)
        {
            var employee = await _repository.GetByIdAsync(id);
            if (employee != null)
            {
                employee.IsDeleted = true;
                employee.UpdateDate = DateTime.Now; // UpdateDate -> UpdatedDate kontrolü yapın (Entity'nize göre)
                _repository.Update(employee);
            }
        }

        // --- YENİ EKLENEN EmployeeType İŞLEMLERİ ---
        public async Task<List<EmployeeType>> GetEmployeeTypesAsync()
        {
            var types = await _typeRepository.GetAllAsync();
            return types.ToList();
        }

        public async Task<EmployeeType> GetEmployeeTypeByIdAsync(int id)
        {
            return await _typeRepository.GetByIdAsync(id);
        }

        public async Task CreateEmployeeTypeAsync(EmployeeType employeeType)
        {
            await _typeRepository.AddAsync(employeeType);
        }

        public async Task UpdateEmployeeTypeAsync(EmployeeType employeeType)
        {
            _typeRepository.Update(employeeType);
        }

        public async Task DeleteEmployeeTypeAsync(int id)
        {
            var type = await _typeRepository.GetByIdAsync(id);
            if (type != null)
            {
                _typeRepository.Delete(type);
            }
        }
    }
}