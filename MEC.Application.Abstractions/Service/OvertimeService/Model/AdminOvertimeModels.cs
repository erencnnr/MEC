namespace MEC.Application.Abstractions.Service.OvertimeService.Model
{
    public class AdminOvertimeRequestListQueryModel
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? Status { get; set; }
    }

    public class AdminOvertimeRequestItemModel : OvertimeHistoryItemModel
    {
        public bool CanTakeAction { get; set; }
    }

    public class OvertimeStatusUpdateRequestModel
    {
        public int OvertimeRequestId { get; set; }
        public int Status { get; set; }
        public string CurrentUser { get; set; } = string.Empty;
        public string? DecisionBy { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string MethodName { get; set; } = "UpdateOvertimeStatus";
    }
}
