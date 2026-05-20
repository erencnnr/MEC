namespace MEC.Application.Abstractions.Service.NotificationService.Model
{
    public abstract class WorkflowNotificationContextModel
    {
        public string TriggeredByUser { get; set; } = string.Empty;
        public string IpAddress { get; set; } = "unknown";
    }

    public class LeaveRequestCreatedNotificationModel : WorkflowNotificationContextModel
    {
        public int LeaveId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeEmail { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class LeaveRequestCancelledNotificationModel : WorkflowNotificationContextModel
    {
        public int LeaveId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string CancelledBy { get; set; } = string.Empty;
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
    }
}
