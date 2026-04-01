using System;

namespace MEC.Portal.Models
{
    public class AdminLeaveRequestViewModel
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string LeaveType { get; set; } = string.Empty;
        public decimal RequestedDays { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int Status { get; set; }
        public decimal RemainingLeaveDays { get; set; }
    }
}
