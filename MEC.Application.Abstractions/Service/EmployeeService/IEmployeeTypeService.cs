using MEC.Application.Abstractions.Application;
using MEC.Domain.Entity.Employee;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MEC.Application.Abstractions.Service.EmployeeService
{
    public interface IEmployeeTypeService : IApplicationService
    {
        Task<List<EmployeeType>> GetEmployeeTypeListAsync();
        Task<EmployeeType> GetEmployeeTypeByIdAsync(int id);
        Task CreateEmployeeTypeAsync(EmployeeType employeeType);
        Task UpdateEmployeeTypeAsync(EmployeeType employeeType);
        Task<string> DeleteEmployeeTypeAsync(int id);
    }
}
