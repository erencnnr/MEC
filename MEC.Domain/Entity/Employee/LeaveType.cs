using MEC.Domain.Common;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.Leave
{
    [Table("leave_type")]
    public class LeaveType : BaseEntity
    {
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("code")]
        public string Code { get; set; } = string.Empty;

        [Column("is_active")]
        public bool IsActive { get; set; }

        public ICollection<Leave> Leaves { get; set; } = new List<Leave>();
    }
}
