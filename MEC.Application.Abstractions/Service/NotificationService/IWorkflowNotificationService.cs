using MEC.Application.Abstractions.Service.NotificationService.Model;

namespace MEC.Application.Abstractions.Service.NotificationService
{
    public interface IWorkflowNotificationService
    {
        Task NotifyLeaveRequestCreatedAsync(LeaveRequestCreatedNotificationModel model);
        Task NotifyLeaveRequestCancelledAsync(LeaveRequestCancelledNotificationModel model);
        Task NotifyLeaveRequestDecisionAsync(LeaveRequestDecisionNotificationModel model);
        Task NotifyOvertimeRequestCreatedAsync(OvertimeRequestCreatedNotificationModel model);
        Task NotifyOvertimeRequestCancelledAsync(OvertimeRequestCancelledNotificationModel model);
        Task NotifyOvertimeRequestDecisionAsync(OvertimeRequestDecisionNotificationModel model);
    }
}
