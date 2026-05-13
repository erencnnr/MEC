using System.ComponentModel.DataAnnotations;

namespace MEC.Portal.Models
{
    public class OvertimeRequestViewModel
    {
        public string Date { get; set; } = string.Empty;

        public string StartTime { get; set; } = "18:00";

        public string EndTime { get; set; } = "19:00";

        public decimal RequestedHours { get; set; }

        [Required(ErrorMessage = "Mesai açıklaması zorunludur.")]
        public string Reason { get; set; } = string.Empty;
    }

    public class OvertimeHistoryViewModel
    {
        public List<OvertimeHistoryItemViewModel> Items { get; set; } = new();
        public List<int> YearOptions { get; set; } = new();
        public List<OvertimeStatusFilterOptionViewModel> StatusOptions { get; set; } = new();
        public int? SelectedYear { get; set; }
        public int? SelectedStatus { get; set; }
        public string SelectedSort { get; set; } = "created_desc";
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;
    }

    public class OvertimeHistoryItemViewModel
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal RequestedHours { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int Status { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusTone { get; set; } = "pending";
        public string DecisionDisplay { get; set; } = "-";
    }

    public class OvertimeHistoryDetailViewModel
    {
        public OvertimeHistoryItemViewModel Item { get; set; } = new();
    }

    public class OvertimeStatusFilterOptionViewModel
    {
        public int Value { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public class AdminOvertimeRequestViewModel
    {
        public int Id { get; set; }
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
        public bool CanTakeAction { get; set; }
    }

    public class AdminOvertimeRequestListViewModel
    {
        public List<AdminOvertimeRequestViewModel> Items { get; set; } = new();
        public List<OvertimeStatusFilterOptionViewModel> StatusOptions { get; set; } = new();
        public int? SelectedStatus { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;
    }

    public class AdminOvertimeRequestDetailViewModel
    {
        public AdminOvertimeRequestViewModel Item { get; set; } = new();
    }
}
