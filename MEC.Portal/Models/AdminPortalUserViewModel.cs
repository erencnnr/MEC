using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace MEC.Portal.Models
{
    public class AdminPortalUserListViewModel
    {
        public List<AdminPortalUserListItemViewModel> Items { get; set; } = new();
        public string Status { get; set; } = "active";
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
        public string LocationName { get; set; } = string.Empty;
        public decimal LeaveDays { get; set; }
        public DateTime HireDate { get; set; }
        public bool IsAdmin { get; set; }
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
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime HireDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; }

        public int? LocationId { get; set; }

        [Range(typeof(decimal), "0", "9999")]
        public decimal LeaveDays { get; set; }

        public bool IsAdmin { get; set; }

        public bool IsDeleted { get; set; }
        public List<SelectListItem> LocationOptions { get; set; } = new();
        public string FullName => string.Join(" ", new[] { FirstName, LastName }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
    }
}
