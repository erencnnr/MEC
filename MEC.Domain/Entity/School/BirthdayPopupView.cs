using MEC.Domain.Common;
using MEC.Domain.Entity.Employee;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.School
{
    [Table("birthday_popup_view")]
    public class BirthdayPopupView : BaseEntity
    {
        [Column("employee_portal_id")]
        public int EmployeePortalId { get; set; }

        [Column("shown_year")]
        public int ShownYear { get; set; }

        [Column("shown_at")]
        public DateTime ShownAt { get; set; }

        [ForeignKey(nameof(EmployeePortalId))]
        public EmployeePortal? EmployeePortal { get; set; }
    }
}
