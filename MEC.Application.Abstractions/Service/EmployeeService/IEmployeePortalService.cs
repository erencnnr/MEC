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
        Task<PortalSelfEditModel?> GetSelfProfileEditAsync(string email);
        Task<EmployeePortal?> GetActivePortalUserByEmailAsync(string email);
        Task<OperationResultModel> UpdatePortalUserAsync(PortalUserEditModel model);
        Task<OperationResultModel> UpdateSelfProfileAsync(string email, PortalSelfEditModel model);
        Task<bool> RequiresProfileCompletionAsync(string email);
        Task<List<LocationOptionModel>> GetLocationOptionsAsync();
    }
}
