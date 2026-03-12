using MEC.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using MEC.Domain.Entity.Leave; // Bu satırın olduğundan emin olun

namespace MEC.Domain.Entity.Employee
{
    [Table("employee")]
    public class Employee : BaseEntity
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsDeleted { get; set; } = false;
        public int? EmployeeTypeId { get; set; }
        public EmployeeType? EmployeeType { get; set; }

        // HATA VEREN SATIRI BU ŞEKİLDE DEĞİŞTİRİN:
        public ICollection<MEC.Domain.Entity.Leave.Leave>? Leaves { get; set; }
    }
}