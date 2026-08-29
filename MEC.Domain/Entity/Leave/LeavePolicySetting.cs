using MEC.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.Leave
{
    [Table("leave_policy_setting")]
    public class LeavePolicySetting : BaseEntity
    {
        [Column("effective_from")]
        public DateTime EffectiveFrom { get; set; }

        [Column("count_saturday")]
        public bool CountSaturday { get; set; }
    }
}
