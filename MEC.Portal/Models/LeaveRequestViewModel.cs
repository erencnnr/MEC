namespace MEC.Portal.Models
{
    public class LeaveRequestViewModel
    {
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public decimal RequestedDays { get; set; }
        public string LeaveType { get; set; } = string.Empty;
        public IFormFile? Attachment { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
