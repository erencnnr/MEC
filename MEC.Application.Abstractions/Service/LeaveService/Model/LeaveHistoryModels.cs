using MEC.Application.Abstractions.Common.Models;

namespace MEC.Application.Abstractions.Service.LeaveService.Model
{
    public class LeaveHistoryQueryModel
    {
        public string UserEmail { get; set; } = string.Empty;
        public int? Year { get; set; }
        public int? Status { get; set; }
        public int? LeaveTypeId { get; set; }
        public string Sort { get; set; } = "created_desc";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class LeaveHistoryResultModel : PagedResultModel<LeaveHistoryItemModel>
    {
        public List<int> YearOptions { get; set; } = new();
        public List<LeaveTypeOptionModel> LeaveTypeOptions { get; set; } = new();
        public List<LeaveStatusOptionModel> StatusOptions { get; set; } = new();
        public int? SelectedYear { get; set; }
        public int? SelectedStatus { get; set; }
        public int? SelectedLeaveTypeId { get; set; }
        public string SelectedSort { get; set; } = "created_desc";
        public string? ErrorMessage { get; set; }
    }

    public class LeaveHistoryItemModel
    {
        public int Id { get; set; }
        public int LeaveTypeId { get; set; }
        public string LeaveType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal RequestedDays { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int Status { get; set; }
        public decimal RemainingLeaveDays { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusTone { get; set; } = "pending";
        public string DecisionDisplay { get; set; } = "-";
    }
}
