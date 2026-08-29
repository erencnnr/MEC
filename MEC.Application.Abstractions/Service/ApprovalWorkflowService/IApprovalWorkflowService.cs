using MEC.Application.Abstractions.Service.ApprovalWorkflowService.Model;

namespace MEC.Application.Abstractions.Service.ApprovalWorkflowService
{
    public interface IApprovalWorkflowService
    {
        Task<ApprovalActorModel?> GetActorAsync(string email);
        Task<ApprovalRouteModel?> ResolveRouteAsync(int employeePortalId);
        Task<Dictionary<int, ApprovalRouteModel>> ResolveRoutesAsync(IEnumerable<int> employeePortalIds);
    }
}
