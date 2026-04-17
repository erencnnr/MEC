using MEC.Application.Abstractions.Common.Models;

namespace MEC.Application.Abstractions.Service.LeaveService.Model
{
    public class AdminLeaveRequestListQueryModel
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? Status { get; set; }
    }

    public class AdminLeaveRequestItemModel : LeaveHistoryItemModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public bool CanTakeAction { get; set; }
    }

    public class AdminLeaveReportQueryModel
    {
        public int? EmployeeId { get; set; }
        public int? LeaveTypeId { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
    }

    public class AdminLeaveReportResultModel
    {
        public List<AdminLeaveReportItemModel> Items { get; set; } = new();
        public List<AdminLeaveReportFilterOptionModel> EmployeeOptions { get; set; } = new();
        public List<AdminLeaveReportFilterOptionModel> LeaveTypeOptions { get; set; } = new();
        public int? SelectedEmployeeId { get; set; }
        public int? SelectedLeaveTypeId { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public int TotalCount { get; set; }
    }

    public class AdminLeaveReportItemModel
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string LeaveType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal RequestedDays { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusTone { get; set; } = "pending";
        public DateTime? CreatedDate { get; set; }
    }

    public class AdminLeaveReportFilterOptionModel
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public class LeaveStatusUpdateRequestModel
    {
        public int LeaveId { get; set; }
        public int Status { get; set; }
        public string CurrentUser { get; set; } = string.Empty;
        public string? DecisionBy { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string MethodName { get; set; } = "UpdateLeaveStatus";
    }

    public class LeaveReportExportModel
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    }
}
