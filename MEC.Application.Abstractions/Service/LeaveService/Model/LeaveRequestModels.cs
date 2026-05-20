using MEC.Application.Abstractions.Common.Models;

namespace MEC.Application.Abstractions.Service.LeaveService.Model
{
    public class LeaveRequestCreateModel
    {
        public string UserEmail { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int LeaveTypeId { get; set; }
        public decimal RequestedDays { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class LeaveRequestCreateResultModel
    {
        public int LeaveId { get; set; }
    }

    public class LeaveRequestCreatedDispatchModel
    {
        public int LeaveId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string IpAddress { get; set; } = "unknown";
    }

    public class LeaveCancelRequestModel
    {
        public int LeaveId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string CurrentUser { get; set; } = string.Empty;
        public string? CancelledBy { get; set; }
        public string IpAddress { get; set; } = "unknown";
        public string MethodName { get; set; } = "CancelLeaveRequest";
    }

    public class LeaveRequestValidationModel : OperationResultModel
    {
        public Dictionary<string, string> FieldErrors { get; set; } = new();
        public decimal RequestedDays { get; set; }
        public LeaveTypeOptionModel? SelectedLeaveType { get; set; }
    }
}
