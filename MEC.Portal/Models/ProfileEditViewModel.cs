using MEC.Domain.Common.Enum;
using System.ComponentModel.DataAnnotations;

namespace MEC.Portal.Models
{
    public class ProfileEditViewModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime HireDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; }

        [Required(ErrorMessage = "Telefon alanı zorunludur.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [DataType(DataType.MultilineText)]
        public string? AddressText { get; set; }

        public MaritalStatusType? MaritalStatus { get; set; }
        public string? EducationUniversity { get; set; }
        public string? EducationFaculty { get; set; }
        public string? EducationDepartment { get; set; }
        public bool RequireCompletion { get; set; }
        public List<ProfileChildInputViewModel> Children { get; set; } = new();
        public string FullName => string.Join(" ", new[] { FirstName, LastName }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
    }

    public class ProfileChildInputViewModel
    {
        public ChildGenderType? Gender { get; set; }

        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }
    }
}
