namespace MEC.Application.Abstractions.Service.ApprovalWorkflowService.Model
{
    public class ApprovalWorkflowSettings
    {
        public string FinalApproverEmail { get; set; } = "mustafa.meral@mecokullari.k12.tr";
    }

    public class ApprovalRecipientModel
    {
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    public class ApprovalActorModel : ApprovalRecipientModel
    {
        public int EmployeePortalId { get; set; }
        public bool IsAdministrator { get; set; }
        public bool IsLocationManager { get; set; }
        public bool IsFinalApprover { get; set; }
        public List<int> LocationIds { get; set; } = new();
    }

    public class ApprovalRouteModel
    {
        public int EmployeePortalId { get; set; }
        public string EmployeeEmail { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public bool EmployeeIsLocationManager { get; set; }
        public List<int> LocationIds { get; set; } = new();
        public string LocationNames { get; set; } = string.Empty;
        public List<ApprovalRecipientModel> ManagerApprovers { get; set; } = new();
        public ApprovalRecipientModel FinalApprover { get; set; } = new();
        public bool HasLocation => LocationIds.Count > 0;
        public bool RequiresManagerApproval => ManagerApprovers.Count > 0;
    }
}
