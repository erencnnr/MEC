using MEC.Domain.Common;
using MEC.Domain.Entity.Employee;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.Leave
{
    [Table("leave_agreement")]
    public class LeaveAgreement : BaseEntity
    {
        [Column("employee_portal_id")]
        public int EmployeePortalId { get; set; }

        [ForeignKey(nameof(EmployeePortalId))]
        public EmployeePortal? EmployeePortal { get; set; }

        [Column("agreed_leave_days", TypeName = "decimal(10,2)")]
        public decimal AgreedLeaveDays { get; set; }

        [Column("is_signed")]
        public bool IsSigned { get; set; }
    }
}
