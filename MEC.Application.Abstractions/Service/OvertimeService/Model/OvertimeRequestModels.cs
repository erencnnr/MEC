using MEC.Application.Abstractions.Common.Models;

namespace MEC.Application.Abstractions.Service.OvertimeService.Model
{
    public class OvertimeRequestCreateModel
    {
        public string UserEmail { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class OvertimeRequestCreateResultModel
    {
        public int OvertimeRequestId { get; set; }
    }

    public class OvertimeRequestValidationModel : OperationResultModel
    {
        public Dictionary<string, string> FieldErrors { get; set; } = new();
        public decimal RequestedHours { get; set; }
    }
}
