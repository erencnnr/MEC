namespace MEC.Portal.Models
{
    public class AdminLeaveBalanceListViewModel
    {
        public List<AdminLeaveBalanceViewModel> Items { get; set; } = new();
        public List<AdminLeaveBalanceLocationViewModel> LocationOptions { get; set; } = new();
        public bool CanViewAllLocations { get; set; }
        public string SearchText { get; set; } = string.Empty;
        public int? SelectedLocationId { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 20;
        public bool HasActiveFilters => !string.IsNullOrWhiteSpace(SearchText) || SelectedLocationId.HasValue;
    }

    public class AdminLeaveBalanceViewModel
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

    public class AdminLeaveBalanceLocationViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
