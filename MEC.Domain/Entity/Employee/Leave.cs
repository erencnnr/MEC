using MEC.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;


namespace MEC.Domain.Entity.Leave
{
    [Table("leaves")]
    public class Leave : BaseEntity
    {
        public int EmployeeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        [Column("leave_type_id")]
        public int LeaveTypeId { get; set; }
        [Column("requested_days", TypeName = "decimal(10,2)")]
        public decimal RequestedDays { get; set; }
        [Column("remaining_leave_days", TypeName = "decimal(10,2)")]
        public decimal RemainingLeaveDays { get; set; }
        [Column("minimum_block_exception_requested")]
        public bool MinimumBlockExceptionRequested { get; set; }
        public string Reason { get; set; }
        public int Status { get; set; }
        [Column("decision_by")]
        public string? DecisionBy { get; set; }
        [Column("decision_date")]
        public DateTime? DecisionDate { get; set; }
        [Column("manager_decision_by")]
        public string? ManagerDecisionBy { get; set; }
        [Column("manager_decision_date")]
        public DateTime? ManagerDecisionDate { get; set; }
        [ForeignKey(nameof(EmployeeId))]
        public MEC.Domain.Entity.Employee.EmployeePortal? EmployeePortal { get; set; }
        public LeaveType? LeaveType { get; set; }
    }
}
