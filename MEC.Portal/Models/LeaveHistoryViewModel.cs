namespace MEC.Portal.Models
{
    public class LeaveHistoryViewModel
    {
        public List<LeaveHistoryItemViewModel> LeaveHistory { get; set; } = new();
        public List<int> YearOptions { get; set; } = new();
        public List<LeaveTypeOptionViewModel> LeaveTypeOptions { get; set; } = new();
        public List<LeaveStatusFilterOptionViewModel> StatusOptions { get; set; } = new();
        public int? SelectedYear { get; set; }
        public int? SelectedStatus { get; set; }
        public int? SelectedLeaveTypeId { get; set; }
        public string SelectedSort { get; set; } = "created_desc";
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;
    }

    public class LeaveStatusFilterOptionViewModel
    {
        public int Value { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public class LeaveHistoryDetailViewModel
    {
        public LeaveHistoryItemViewModel Item { get; set; } = new();
    }
}
