using MEC.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.Employee
{
    [Table("location")]
    public class Location : BaseEntity
    {
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("manager_employee_portal_id")]
        public int? ManagerEmployeePortalId { get; set; }

        public EmployeePortal? Manager { get; set; }

        public ICollection<EmployeePortalLocation> EmployeePortalLocations { get; set; } = new List<EmployeePortalLocation>();
    }
}
