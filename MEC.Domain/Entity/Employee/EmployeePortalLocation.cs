using MEC.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.Employee
{
    [Table("employee_portal_location")]
    public class EmployeePortalLocation : BaseEntity
    {
        [Column("employee_portal_id")]
        public int EmployeePortalId { get; set; }

        [Column("location_id")]
        public int LocationId { get; set; }

        [ForeignKey(nameof(EmployeePortalId))]
        public EmployeePortal? EmployeePortal { get; set; }

        [ForeignKey(nameof(LocationId))]
        public Location? Location { get; set; }
    }
}
