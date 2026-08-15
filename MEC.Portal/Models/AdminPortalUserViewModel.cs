using MEC.Domain.Common.Enum;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace MEC.Portal.Models
{
    public class AdminPortalUserListViewModel
    {
        public List<AdminPortalUserListItemViewModel> Items { get; set; } = new();
        public string Status { get; set; } = "active";
        public string SearchTerm { get; set; } = string.Empty;
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
    }

    public class AdminPortalUserListItemViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string LocationNames { get; set; } = string.Empty;
        public decimal LeaveDays { get; set; }
        public DateTime? HireDate { get; set; }
        public DateTime? TerminationDate { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsManager { get; set; }
        public bool IsDeleted { get; set; }
        public string StatusLabel => IsDeleted ? "Pasif" : "Aktif";
        public string StatusTone => IsDeleted ? "rejected" : "approved";
    }

    public class AdminPortalUserEditViewModel
    {
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon alanı zorunludur.")]
        [RegularExpression(@"^(?:0\d{10}|0\d{3} \d{3} \d{2} \d{2})$", ErrorMessage = "Telefon numarası 0 ile başlayan 11 haneli olmalıdır.")]
        public string PhoneNumber { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? HireDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? TerminationDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        public List<int> LocationIds { get; set; } = new();

        [DataType(DataType.MultilineText)]
        public string? AddressText { get; set; }

        public MaritalStatusType? MaritalStatus { get; set; }

        public string? EducationUniversity { get; set; }

        public string? EducationFaculty { get; set; }

        public string? EducationDepartment { get; set; }

        [Range(typeof(decimal), "0", "9999")]
        public decimal LeaveDays { get; set; }

        public bool IsAdmin { get; set; }

        public bool IsManager { get; set; }

        public bool IsDeleted { get; set; }
        public bool ActiveLoanWarningAccepted { get; set; }
        public List<AdminPortalUserActiveLoanViewModel> ActiveLoans { get; set; } = new();
        public int LoanRecordCount { get; set; }
        public bool HasActiveLoans => ActiveLoans.Count > 0;
        public bool HasLoanRecords => LoanRecordCount > 0;
        public string PreferredLoanTab => HasActiveLoans ? "active" : "history";
        public List<SelectListItem> LocationOptions { get; set; } = new();
        public List<ProfileChildInputViewModel> Children { get; set; } = new();
        public string FullName => string.Join(" ", new[] { FirstName, LastName }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
    }

    public class AdminPortalUserActiveLoanViewModel
    {
        public int LoanId { get; set; }
        public int AssetId { get; set; }
        public string AssetName { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
    }
}
