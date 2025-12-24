using MEC.Application.Abstractions.Application;
using MEC.Domain.Entity.Employee; // <--- BU SATIR ÇOK ÖNEMLİ (EmployeeType'ı tanıması için)
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MEC.Application.Abstractions.Service.EmployeeService
{
    public interface IEmployeeService : IApplicationService
    {
        Task<List<Employee>> GetEmployeeListAsync();

        Task CreateEmployeeAsync(Employee employee);

        // Bu metodun çalışması için yukarıdaki 'using MEC.Domain.Entity.Employee;' şarttır.
        Task<List<EmployeeType>> GetEmployeeTypesAsync();

        Task DeleteEmployeeAsync(int id);
    }
}