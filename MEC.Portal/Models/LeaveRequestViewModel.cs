namespace MEC.Portal.Models
{
    public class LeaveRequestViewModel
    {
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public decimal RequestedDays { get; set; }
        public int? LeaveTypeId { get; set; }
        public IFormFile? Attachment { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<LeaveTypeOptionViewModel> LeaveTypes { get; set; } = new();
        public List<HolidayCalendarItemViewModel> Holidays { get; set; } = new();
    }

    public class HolidayCalendarItemViewModel
    {
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
