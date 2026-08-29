using MEC.Application.Abstractions.Common.Models;

namespace MEC.Application.Abstractions.Service.LeaveService.Model
{
    public class AdminLeaveBalanceQueryModel
    {
        public string CurrentUserEmail { get; set; } = string.Empty;
        public string? SearchText { get; set; }
        public int? LocationId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class AdminLeaveBalanceResultModel : PagedResultModel<AdminLeaveBalanceItemModel>
    {
        public bool IsAuthorized { get; set; }
        public bool CanViewAllLocations { get; set; }
        public string SearchText { get; set; } = string.Empty;
        public int? SelectedLocationId { get; set; }
        public List<AdminLeaveBalanceLocationOptionModel> LocationOptions { get; set; } = new();
    }

    public class AdminLeaveBalanceItemModel
    {
        public int EmployeePortalId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string LocationNames { get; set; } = string.Empty;
        public DateTime? HireDate { get; set; }
        public int CompletedServiceYears { get; set; }
        public decimal AnnualEntitlementDays { get; set; }
        public decimal CurrentBalance { get; set; }
        public DateTime? NextEntitlementDate { get; set; }
        public bool HasReconciliation { get; set; }
        public DateTime? BalanceAsOfDate { get; set; }
        public decimal ReconciledOpeningBalance { get; set; }
        public decimal CurrentYearEarnedDays { get; set; }
        public decimal CurrentYearUsedDays { get; set; }
    }

    public class AdminLeaveBalanceLocationOptionModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
