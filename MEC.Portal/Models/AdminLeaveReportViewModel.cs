using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MEC.Portal.Models
{
    public class AdminLeaveReportViewModel
    {
        public int? EmployeeId { get; set; }
        public string LeaveType { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int TotalRecords { get; set; }
        public int TotalDays { get; set; }
        public int ApprovedCount { get; set; }
        public int PendingCount { get; set; }
        public int RejectedCount { get; set; }
        public string AppliedPeriodLabel { get; set; } = string.Empty;
        public List<SelectListItem> EmployeeOptions { get; set; } = new();
        public List<SelectListItem> LeaveTypeOptions { get; set; } = new();
        public List<AdminLeaveReportItemViewModel> Reports { get; set; } = new();
    }

    public class AdminLeaveReportItemViewModel
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string LeaveType { get; set; } = string.Empty;
        public int RequestedDays { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Status { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime? CreatedDate { get; set; }
    }
}
