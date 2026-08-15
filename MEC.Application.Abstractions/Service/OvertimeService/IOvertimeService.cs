using MEC.Application.Abstractions.Common.Models;
using MEC.Application.Abstractions.Service.OvertimeService.Model;

namespace MEC.Application.Abstractions.Service.OvertimeService
{
    public interface IOvertimeService
    {
        Task<OvertimeRequestValidationModel> ValidateOvertimeRequestAsync(OvertimeRequestCreateModel request);
        Task<OperationResultModel<OvertimeRequestCreateResultModel>> CreateOvertimeRequestAsync(OvertimeRequestCreateModel request);
        Task<OperationResultModel> CancelOvertimeRequestAsync(OvertimeCancelRequestModel request);
        Task<OvertimeHistoryResultModel> GetOvertimeHistoryAsync(OvertimeHistoryQueryModel query);
        Task<OvertimeHistoryItemModel?> GetOvertimeHistoryDetailAsync(string userEmail, int overtimeRequestId);
        Task<PagedResultModel<AdminOvertimeRequestItemModel>> GetAdminOvertimeRequestsAsync(AdminOvertimeRequestListQueryModel query);
        Task<AdminOvertimeRequestItemModel?> GetAdminOvertimeRequestDetailAsync(int id, string currentUserEmail);
        Task<AdminOvertimeReportResultModel> GetAdminOvertimeReportAsync(AdminOvertimeReportQueryModel query);
        Task<OvertimeReportExportModel> ExportAdminOvertimeReportAsync(AdminOvertimeReportQueryModel query);
        Task<OperationResultModel> UpdateOvertimeStatusAsync(OvertimeStatusUpdateRequestModel request);
    }
}
