using MEC.Application.Abstractions.Common.Models;

namespace MEC.Application.Abstractions.Service.OvertimeService.Model
{
    public class OvertimeHistoryQueryModel
    {
        public string UserEmail { get; set; } = string.Empty;
        public int? Year { get; set; }
        public int? Status { get; set; }
        public string Sort { get; set; } = "created_desc";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class OvertimeHistoryResultModel : PagedResultModel<OvertimeHistoryItemModel>
    {
        public List<int> YearOptions { get; set; } = new();
        public List<OvertimeStatusOptionModel> StatusOptions { get; set; } = new();
        public int? SelectedYear { get; set; }
        public int? SelectedStatus { get; set; }
        public string SelectedSort { get; set; } = "created_desc";
        public string? ErrorMessage { get; set; }
    }

    public class OvertimeHistoryItemModel
    {
        public int Id { get; set; }
        public int EmployeePortalId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal RequestedHours { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int Status { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusTone { get; set; } = "pending";
        public string DecisionDisplay { get; set; } = "-";
        public bool CanCancel { get; set; }
    }

    public class OvertimeStatusOptionModel
    {
        public int Value { get; set; }
        public string Label { get; set; } = string.Empty;
    }
}
