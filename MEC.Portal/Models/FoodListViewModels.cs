namespace MEC.Portal.Models
{
    public class FoodListViewModel
    {
        public List<FoodListMonthOptionViewModel> Months { get; set; } = new();
        public int? SelectedMonthId { get; set; }
        public string SelectedMonthLabel { get; set; } = string.Empty;
        public string PdfPreviewUrl { get; set; } = string.Empty;
        public string PdfFileName { get; set; } = string.Empty;
        public List<FoodListDayOptionViewModel> Days { get; set; } = new();
        public FoodListSelectedDayViewModel? SelectedDay { get; set; }
    }

    public class FoodListMonthOptionViewModel
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public class FoodListDayOptionViewModel
    {
        public string QueryValue { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public class FoodListSelectedDayViewModel
    {
        public DateTime MenuDate { get; set; }
        public string RawItemsText { get; set; } = string.Empty;
        public List<string> MenuItems { get; set; } = new();
        public int PreviewPageNumber { get; set; } = 1;
    }
}
