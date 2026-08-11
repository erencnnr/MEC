using MEC.Domain.Common;
using MEC.Domain.Common.Enum;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.Employee
{
    [Table("employee_portal")]
    public class EmployeePortal : BaseEntity
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime? HireDate { get; set; }
        public DateTime? TerminationDate { get; set; }
        public DateTime? BirthDate { get; set; }

        [Column("address_text")]
        public string? AddressText { get; set; }

        [Column("marital_status")]
        public MaritalStatusType? MaritalStatus { get; set; }

        [Column("education_university")]
        public string? EducationUniversity { get; set; }

        [Column("education_faculty")]
        public string? EducationFaculty { get; set; }

        [Column("education_department")]
        public string? EducationDepartment { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal LeaveDays { get; set; }

        public bool IsAdmin { get; set; }
        public bool IsDeleted { get; set; }

        public ICollection<EmployeePortalChild> Children { get; set; } = new List<EmployeePortalChild>();
        public ICollection<EmployeePortalLocation> EmployeePortalLocations { get; set; } = new List<EmployeePortalLocation>();
    }
}
