using MEC.Application.Abstractions.Common.Models;
using MEC.Domain.Entity.Employee;

namespace MEC.Application.Abstractions.Service.EmployeeService.Model
{
    public class PortalUserListQueryModel
    {
        public string? Status { get; set; } = "active";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class PortalUserListItemModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int? LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public decimal LeaveDays { get; set; }
        public DateTime HireDate { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class PortalUserListResultModel : PagedResultModel<PortalUserListItemModel>
    {
        public string Status { get; set; } = "active";
    }

    public class PortalUserEditModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime HireDate { get; set; }
        public DateTime BirthDate { get; set; }
        public int? LocationId { get; set; }
        public decimal LeaveDays { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class LocationOptionModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ProfileSummaryModel
    {
        public EmployeePortal? Profile { get; set; }
        public int PendingAnnualLeaveCount { get; set; }
        public decimal PendingAnnualLeaveDays { get; set; }
    }
}
