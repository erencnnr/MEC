using System;
using System.Collections.Generic;

namespace MEC.Portal.Models
{
    public class AdminLeaveRequestViewModel
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int LeaveTypeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string LeaveType { get; set; } = string.Empty;
        public decimal RequestedDays { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int Status { get; set; }
        public decimal RemainingLeaveDays { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusTone { get; set; } = "pending";
        public string DecisionDisplay { get; set; } = "-";
        public bool CanTakeAction { get; set; }
    }

    public class AdminLeaveRequestListViewModel
    {
        public List<AdminLeaveRequestViewModel> Items { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;
    }

    public class AdminLeaveRequestDetailViewModel
    {
        public AdminLeaveRequestViewModel Item { get; set; } = new();
    }
}
