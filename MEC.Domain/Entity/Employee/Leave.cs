using MEC.Domain.Common;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.Employee
{
    [Table("leave")]
    public class Leave : BaseEntity
    {
        // İzni talep eden personelin ID'si ve referansı
        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        // İzin başlangıç ve bitiş tarihleri
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // İzin açıklaması veya nedeni
        public string? Reason { get; set; }

        // İsteğe bağlı: İzin durumunu (Bekliyor, Onaylandı, Reddedildi) takip etmek için bir alan eklenebilir.
        // public bool IsApproved { get; set; } = false;
    }
}
