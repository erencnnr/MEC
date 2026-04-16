namespace MEC.Application.Abstractions.Service.LeaveService.Model
{
    public class BulkLeaveUpdatedUserModel
    {
        public string Email { get; set; } = string.Empty;
        public decimal NewLeaveDays { get; set; }
    }
}
