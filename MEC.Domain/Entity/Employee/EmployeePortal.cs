using MEC.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.Employee
{
    // BaseEntity'den miras alarak Id, CreatedDate gibi temel kolonları otomatik alır
    [Table("employee_portal")]
    public class EmployeePortal : BaseEntity
    {
        public string FirstName { get; set; }       // Ad
        public string LastName { get; set; }        // Soyad
        public string PhoneNumber { get; set; }     // Telefon
        public string Email { get; set; }           // Mail (Giriş yapılan mail ile eşleşecek)
        public DateTime HireDate { get; set; }      // İşe Giriş Tarihi
        public DateTime BirthDate { get; set; }     // Doğum Günü
        public int LeaveDays { get; set; }          // İzin Gün Sayısı
    }
}