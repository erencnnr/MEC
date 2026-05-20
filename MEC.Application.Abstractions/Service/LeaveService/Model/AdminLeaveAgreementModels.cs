using System;

namespace MEC.Application.Abstractions.Service.LeaveService.Model
{
    public class AdminLeaveAgreementListQueryModel
    {
        public string? SearchText { get; set; }
        public bool? IsSigned { get; set; }
        public string? SortOrder { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class AdminLeaveAgreementItemModel
    {
        public int Id { get; set; }
        public int EmployeePortalId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal AgreedLeaveDays { get; set; }
        public bool IsSigned { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class AdminLeaveAgreementUpdateModel
    {
        public int Id { get; set; }
        public decimal AgreedLeaveDays { get; set; }
        public bool IsSigned { get; set; }
    }
}
