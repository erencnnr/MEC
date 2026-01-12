using MEC.Application.Abstractions.Application;
using MEC.Domain.Common;
using MEC.Domain.Entity.Employee;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MEC.Application.Abstractions.Service.EmployeeService
{
    public interface IEmployeeService : IApplicationService
    {
        // Mevcut Metotlar
        Task<List<Employee>> GetAllEmployeesAsync();
        Task<PagedResult<Employee>> GetPagedEmployeeListAsync(string? searchText = null, bool? isAdmin = null, int page = 1, int pageSize = 10);
        Task CreateEmployeeAsync(Employee employee);
        Task DeleteEmployeeAsync(int id); // Daha önce eklemiştik

        // --- YENİ EKLENEN EmployeeType METOTLARI ---
        Task<List<EmployeeType>> GetEmployeeTypesAsync();
        Task<EmployeeType> GetEmployeeTypeByIdAsync(int id);
        Task CreateEmployeeTypeAsync(EmployeeType employeeType);
        Task UpdateEmployeeTypeAsync(EmployeeType employeeType);
        Task DeleteEmployeeTypeAsync(int id);
        Task ActivateEmployeeAsync(int id);
        Task ToggleAdminStatusAsync(int id);
        Task<Employee> GetEmployeeByEmailAsync(string email);
    }
}