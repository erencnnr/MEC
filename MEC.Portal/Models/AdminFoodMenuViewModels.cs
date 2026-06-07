using MEC.Domain.Common.Enum;

namespace MEC.Portal.Models
{
    public class AdminFoodMenuViewModel
    {
        public int ImportYear { get; set; }
        public int ImportMonth { get; set; }
        public List<AdminFoodMenuMonthListItemViewModel> Months { get; set; } = new();
        public AdminFoodMenuEditorViewModel? SelectedMonth { get; set; }
    }

    public class AdminFoodMenuMonthListItemViewModel
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthLabel { get; set; } = string.Empty;
        public FoodMenuMonthStatus Status { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusTone { get; set; } = string.Empty;
        public int DayCount { get; set; }
        public DateTime? ImportedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public bool IsSelected { get; set; }
    }

    public class AdminFoodMenuEditorViewModel
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthLabel { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public int PageCount { get; set; }
        public FoodMenuMonthStatus Status { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusTone { get; set; } = string.Empty;
        public DateTime? ImportedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public List<string> ParseWarnings { get; set; } = new();
        public List<AdminFoodMenuDayEditItemViewModel> Days { get; set; } = new();
    }

    public class AdminFoodMenuDayEditItemViewModel
    {
        public DateTime MenuDate { get; set; }
        public string ItemsText { get; set; } = string.Empty;
        public int? SourcePageNumber { get; set; }
    }
}
