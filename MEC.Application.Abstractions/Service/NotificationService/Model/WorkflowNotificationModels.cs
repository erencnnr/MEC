namespace MEC.Application.Abstractions.Service.NotificationService.Model
{
    public abstract class WorkflowNotificationContextModel
    {
        public string TriggeredByUser { get; set; } = string.Empty;
        public string IpAddress { get; set; } = "unknown";
    }

    public class WorkflowNotificationRecipientModel
    {
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    public class LeaveRequestCreatedNotificationModel : WorkflowNotificationContextModel
    {
        public int LeaveId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeEmail { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string LocationNames { get; set; } = string.Empty;
        public string ApprovalTarget { get; set; } = string.Empty;
        public List<WorkflowNotificationRecipientModel> Approvers { get; set; } = new();
    }

    public class LeaveRequestCancelledNotificationModel : WorkflowNotificationContextModel
    {
        public int LeaveId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string CancelledBy { get; set; } = string.Empty;
        public List<WorkflowNotificationRecipientModel> Approvers { get; set; } = new();
    }

    public class LeaveRequestDecisionNotificationModel : WorkflowNotificationContextModel
    {
        public int LeaveId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeEmail { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string DecisionBy { get; set; } = string.Empty;
        public string DecisionLabel { get; set; } = string.Empty;
        public string LocationNames { get; set; } = string.Empty;
        public bool IsManagerDecision { get; set; }
        public List<WorkflowNotificationRecipientModel> RegionalManagers { get; set; } = new();
        public List<WorkflowNotificationRecipientModel> NextApprovers { get; set; } = new();
    }

    public class OvertimeRequestCreatedNotificationModel : WorkflowNotificationContextModel
    {
        public int OvertimeRequestId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeEmail { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal RequestedHours { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string LocationNames { get; set; } = string.Empty;
        public string ApprovalTarget { get; set; } = string.Empty;
        public List<WorkflowNotificationRecipientModel> Approvers { get; set; } = new();
    }

    public class OvertimeRequestCancelledNotificationModel : WorkflowNotificationContextModel
    {
        public int OvertimeRequestId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal RequestedHours { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string CancelledBy { get; set; } = string.Empty;
        public List<WorkflowNotificationRecipientModel> Approvers { get; set; } = new();
    }

    public class OvertimeRequestDecisionNotificationModel : WorkflowNotificationContextModel
    {
        public int OvertimeRequestId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeEmail { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal RequestedHours { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string DecisionBy { get; set; } = string.Empty;
        public string DecisionLabel { get; set; } = string.Empty;
        public string LocationNames { get; set; } = string.Empty;
        public bool IsManagerDecision { get; set; }
        public List<WorkflowNotificationRecipientModel> RegionalManagers { get; set; } = new();
        public List<WorkflowNotificationRecipientModel> NextApprovers { get; set; } = new();
    }
}
