using MEC.Domain.Common;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;


namespace MEC.Domain.Entity.Employee
{
    public class EmployeeType : BaseEntity
    {
       
       
       
        public string Name { get; set; }

        // Bir Tipi birden fazla çalışan kullanabilir (One-to-Many)
        public ICollection<Employee> Employees { get; set; }
    }
}