namespace MEC.Application.Abstractions.Service.LeaveService.Model
{
    public class BulkLeaveUploadRequestModel
    {
        public Stream ExcelStream { get; set; } = Stream.Null;
        public string CurrentUser { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
    }
}
