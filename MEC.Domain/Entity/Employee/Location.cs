using MEC.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.Employee
{
    [Table("location")]
    public class Location : BaseEntity
    {
        [Column("name")]
        public string Name { get; set; } = string.Empty;
    }
}
