namespace MEC.Application.Abstractions.Service.OvertimeService.Model
{
    public class AdminOvertimeRequestListQueryModel
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? Status { get; set; }
        public string CurrentUserEmail { get; set; } = string.Empty;
    }

    public class AdminOvertimeRequestItemModel : OvertimeHistoryItemModel
    {
        public string LocationNames { get; set; } = string.Empty;
        public string ManagerDecisionDisplay { get; set; } = "-";
        public bool CanTakeAction { get; set; }
    }

    public class OvertimeStatusUpdateRequestModel
    {
        public int OvertimeRequestId { get; set; }
        public int Status { get; set; }
        public string CurrentUser { get; set; } = string.Empty;
        public string? DecisionBy { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string MethodName { get; set; } = "UpdateOvertimeStatus";
        public DateTime? UpdatedStartDate { get; set; }
        public DateTime? UpdatedEndDate { get; set; }
        public string? TimeChangeNote { get; set; }
    }

    public class AdminOvertimeReportQueryModel
    {
        public int? EmployeeId { get; set; }
        public int? Status { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
    }

    public class AdminOvertimeReportResultModel
    {
        public List<AdminOvertimeReportItemModel> Items { get; set; } = new();
        public List<AdminOvertimeReportFilterOptionModel> EmployeeOptions { get; set; } = new();
        public List<OvertimeStatusOptionModel> StatusOptions { get; set; } = new();
        public int? SelectedEmployeeId { get; set; }
        public int? SelectedStatus { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public int TotalCount { get; set; }
    }

    public class AdminOvertimeReportItemModel
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal RequestedHours { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusTone { get; set; } = "pending";
        public DateTime? CreatedDate { get; set; }
    }

    public class AdminOvertimeReportFilterOptionModel
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public class OvertimeReportExportModel
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    }
}
