using MEC.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.School
{
    [Table("school_class")]
    public class SchoolClass : BaseEntity
    {
        // BaseEntity'den Id geliyor olabilir, ancak manuel eklediyseniz kalabilir.
        // public int Id { get; set; } 

        public string Name { get; set; }   // Örn: "9-A"
        

        // Hangi okula ait olduğunu tutan alan (Foreign Key)
        public int SchoolId { get; set; }
    }
}

