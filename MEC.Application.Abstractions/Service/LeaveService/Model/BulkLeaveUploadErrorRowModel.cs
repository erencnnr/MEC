namespace MEC.Application.Abstractions.Service.LeaveService.Model
{
    public class BulkLeaveUploadErrorRowModel
    {
        public int RowNumber { get; set; }
        public string Email { get; set; } = string.Empty;
        public string RawDays { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
