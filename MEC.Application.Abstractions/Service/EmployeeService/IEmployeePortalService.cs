using MEC.Application.Abstractions.Common.Models;
using MEC.Application.Abstractions.Service.EmployeeService.Model;
using MEC.Domain.Entity.Employee;

namespace MEC.Application.Abstractions.Service.EmployeeService
{
    public interface IEmployeePortalService
    {
        Task<List<EmployeePortal>> GetActivePortalUsersAsync();
        Task<EmployeePortal> GetProfileByEmailAsync(string email);
        Task<ProfileSummaryModel> GetProfileSummaryByEmailAsync(string email);
        Task<PortalUserListResultModel> GetPortalUsersAsync(PortalUserListQueryModel query);
        Task<PortalUserEditModel?> GetPortalUserEditAsync(int id);
        Task<EmployeePortal?> GetActivePortalUserByEmailAsync(string email);
        Task<OperationResultModel> UpdatePortalUserAsync(PortalUserEditModel model);
    }
}
