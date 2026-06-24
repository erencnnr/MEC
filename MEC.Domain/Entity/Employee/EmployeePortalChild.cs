using MEC.Domain.Common;
using MEC.Domain.Common.Enum;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.Employee
{
    [Table("employee_portal_child")]
    public class EmployeePortalChild : BaseEntity
    {
        [Column("employee_portal_id")]
        public int EmployeePortalId { get; set; }

        [Column("gender")]
        public ChildGenderType Gender { get; set; }

        [Column("birth_date")]
        public DateTime BirthDate { get; set; }

        [ForeignKey(nameof(EmployeePortalId))]
        public EmployeePortal? EmployeePortal { get; set; }
    }
}
