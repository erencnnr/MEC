namespace MEC.Application.Abstractions.Service.LeaveService.Model
{
    public class BulkLeaveUploadResultModel
    {
        public bool Success { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int UpdatedCount { get; set; }
        public int FailedCount { get; set; }
        public List<BulkLeaveUploadErrorRowModel> FailedRows { get; set; } = new();
        public List<BulkLeaveUpdatedUserModel> UpdatedUsers { get; set; } = new();
    }
}
