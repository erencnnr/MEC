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
        public string Reason { get; set; }
        public int Status { get; set; }

        
    }
}