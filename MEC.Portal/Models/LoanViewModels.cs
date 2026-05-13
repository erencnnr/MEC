using MEC.Application.Abstractions.Service.LoanService.Model;
using MEC.Domain.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MEC.Portal.Models
{
    public class LoanListItemViewModel
    {
        public int Id { get; set; }
        public int AssetId { get; set; }
        public string AssetName { get; set; } = "-";
        public string SerialNumber { get; set; } = "-";
        public string AssignedToName { get; set; } = "-";
        public string AssignedByName { get; set; } = "-";
        public string LoanDate { get; set; } = "-";
        public string? ReturnDate { get; set; }
        public bool IsActive => string.IsNullOrWhiteSpace(ReturnDate);
    }

    public class LoanIndexViewModel
    {
        public PagedResult<LoanListItemViewModel> Loans { get; set; } = new();
        public LoanFilterRequestModel Filters { get; set; } = new();
        public List<SelectListItem> EmployeeOptions { get; set; } = new();
        public List<SelectListItem> AssignedByOptions { get; set; } = new();
        public string ActiveTab => string.IsNullOrWhiteSpace(Filters.ActiveTab) ? "active" : Filters.ActiveTab;
        public string CurrentSort => string.IsNullOrWhiteSpace(Filters.SortOrder) ? "LoanDate_Desc" : Filters.SortOrder;
        public bool HasActiveFilters =>
            !string.IsNullOrWhiteSpace(Filters.SearchText) ||
            (Filters.AssignedToIds != null && Filters.AssignedToIds.Any()) ||
            (Filters.AssignedByIds != null && Filters.AssignedByIds.Any()) ||
            !string.Equals(ActiveTab, "active", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(CurrentSort, "LoanDate_Desc", StringComparison.OrdinalIgnoreCase);
    }
}
