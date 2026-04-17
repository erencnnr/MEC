namespace MEC.Application.Abstractions.Service.LeaveService.Model
{
    public class HolidayCalendarItemModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
