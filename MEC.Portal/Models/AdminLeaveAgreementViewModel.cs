using System;
using System.Collections.Generic;
using System.Globalization;

namespace MEC.Portal.Models
{
    public class AdminLeaveAgreementViewModel
    {
        public int Id { get; set; }
        public int EmployeePortalId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal AgreedLeaveDays { get; set; }
        public bool IsSigned { get; set; }
        public DateTime? CreatedDate { get; set; }

        public string AgreedLeaveDaysInput => AgreedLeaveDays.ToString("0.##", CultureInfo.InvariantCulture);
        public string IsSignedLabel => IsSigned ? "Evet" : "Hayır";
    }

    public class AdminLeaveAgreementListViewModel
    {
        public List<AdminLeaveAgreementViewModel> Items { get; set; } = new();
        public string SearchText { get; set; } = string.Empty;
        public bool? SelectedIsSigned { get; set; }
        public string SortOrder { get; set; } = "CreatedDate_Desc";
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;

        public bool HasActiveFilters =>
            !string.IsNullOrWhiteSpace(SearchText) ||
            SelectedIsSigned.HasValue ||
            string.Equals(SortOrder, "CreatedDate_Asc", StringComparison.OrdinalIgnoreCase);
    }
}
