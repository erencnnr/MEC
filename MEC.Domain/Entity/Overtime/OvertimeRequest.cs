using MEC.Domain.Common;
using MEC.Domain.Entity.Employee;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.Overtime
{
    [Table("overtime_request")]
    public class OvertimeRequest : BaseEntity
    {
        [Column("employee_portal_id")]
        public int EmployeePortalId { get; set; }

        [ForeignKey(nameof(EmployeePortalId))]
        public EmployeePortal? EmployeePortal { get; set; }

        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Column("end_date")]
        public DateTime EndDate { get; set; }

        [Column("requested_hours", TypeName = "decimal(10,2)")]
        public decimal RequestedHours { get; set; }

        [Column("reason")]
        public string Reason { get; set; } = string.Empty;

        [Column("status")]
        public int Status { get; set; }

        [Column("decision_by")]
        public string? DecisionBy { get; set; }

        [Column("decision_date")]
        public DateTime? DecisionDate { get; set; }

        [Column("manager_decision_by")]
        public string? ManagerDecisionBy { get; set; }

        [Column("manager_decision_date")]
        public DateTime? ManagerDecisionDate { get; set; }
    }
}
