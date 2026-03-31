using MEC.Domain.Entity.Employee;
using MEC.Domain.Entity.Leave;

namespace MEC.Portal.Models
{
    public class ProfileViewModel
    {
        public EmployeePortal? Profile { get; set; }
        public int PendingAnnualLeaveCount { get; set; }
        public int PendingAnnualLeaveDays { get; set; }
    }

    public class LeaveHistoryItemViewModel
    {
        public int Id { get; set; }
        public string LeaveType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RequestedDays { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int Status { get; set; }
        public int RemainingLeaveDays { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusTone { get; set; } = "pending";
        public string DecisionDisplay { get; set; } = "-";
    }
}
