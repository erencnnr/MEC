using MEC.Application.Abstractions.Common.Models;
using MEC.Application.Abstractions.Service.LeaveService.Model;
using MEC.Domain.Entity.Leave;

namespace MEC.Application.Abstractions.Service.LeaveService
{
    public interface ILeaveService
    {
        Task<List<Leave>> GetAllLeavesAsync();
        Task<bool> UpdateLeaveStatusAsync(int leaveId, int status, string? decisionBy = null);
        Task<BulkLeaveUploadResultModel> BulkUploadLeaveDaysAsync(BulkLeaveUploadRequestModel request);
        Task<List<LeaveTypeOptionModel>> GetActiveLeaveTypeOptionsAsync();
        Task<List<HolidayCalendarItemModel>> GetHolidayCalendarItemsAsync();
        Task<LeaveRequestValidationModel> ValidateLeaveRequestAsync(LeaveRequestCreateModel request);
        Task<OperationResultModel<LeaveRequestCreateResultModel>> CreateLeaveRequestAsync(LeaveRequestCreateModel request);
        Task DispatchLeaveRequestCreatedNotificationsAsync(LeaveRequestCreatedDispatchModel request);
        Task<OperationResultModel> CancelLeaveRequestAsync(LeaveCancelRequestModel request);
        Task DeleteLeaveAsync(int id);
        Task<LeaveHistoryResultModel> GetLeaveHistoryAsync(LeaveHistoryQueryModel query);
        Task<LeaveHistoryItemModel?> GetLeaveHistoryDetailAsync(string userEmail, int leaveId);
        Task<PagedResultModel<AdminLeaveRequestItemModel>> GetAdminLeaveRequestsAsync(AdminLeaveRequestListQueryModel query);
        Task<AdminLeaveRequestItemModel?> GetAdminLeaveRequestDetailAsync(int id);
        Task<PagedResultModel<AdminLeaveAgreementItemModel>> GetAdminLeaveAgreementsAsync(AdminLeaveAgreementListQueryModel query);
        Task<OperationResultModel> SyncLeaveAgreementsAsync();
        Task<OperationResultModel> UpdateLeaveAgreementAsync(AdminLeaveAgreementUpdateModel model);
        Task<AdminLeaveReportResultModel> GetAdminLeaveReportAsync(AdminLeaveReportQueryModel query);
        Task<LeaveReportExportModel> ExportAdminLeaveReportAsync(AdminLeaveReportQueryModel query);
        Task<OperationResultModel> UpdateLeaveStatusWithLogAsync(LeaveStatusUpdateRequestModel request);
        Task<AdminDashboardModel> GetAdminDashboardAsync(string adminName);
    }
}
