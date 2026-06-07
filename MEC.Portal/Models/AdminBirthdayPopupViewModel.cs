namespace MEC.Portal.Models
{
    public class AdminBirthdayPopupViewModel
    {
        public AdminBirthdayPopupItemViewModel? Item { get; set; }
    }

    public class AdminBirthdayPopupItemViewModel
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime? CreatedDate { get; set; }
    }
}
