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
        [Column("leave_type")]
        public string LeaveType { get; set; }
        [Column("requested_days")]
        public int RequestedDays { get; set; }
        [Column("remaining_leave_days")]
        public int RemainingLeaveDays { get; set; }
        public string Reason { get; set; }
        public int Status { get; set; }

        
    }
}
