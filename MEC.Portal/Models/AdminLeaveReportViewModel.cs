namespace MEC.Portal.Models
{
    public class AdminLeaveReportViewModel
    {
        public List<AdminLeaveReportItemViewModel> Items { get; set; } = new();
        public List<AdminLeaveReportFilterOptionViewModel> EmployeeOptions { get; set; } = new();
        public List<AdminLeaveReportFilterOptionViewModel> LeaveTypeOptions { get; set; } = new();
        public int? SelectedEmployeeId { get; set; }
        public int? SelectedLeaveTypeId { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public int TotalCount { get; set; }
    }

    public class AdminLeaveReportItemViewModel
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

    public class AdminLeaveReportFilterOptionViewModel
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
    }
}
