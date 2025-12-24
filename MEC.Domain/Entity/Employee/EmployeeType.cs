using MEC.Domain.Common;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;


namespace MEC.Domain.Entity.Employee
{
    [Table("employee_type")]
    public class EmployeeType : BaseEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}