using MEC.Domain.Entity.Employee;

namespace MEC.Application.Abstractions.Service.EmployeeService
{
    public interface IEmployeePortalService
    {
        Task<EmployeePortal> GetProfileByEmailAsync(string email);
    }
}

