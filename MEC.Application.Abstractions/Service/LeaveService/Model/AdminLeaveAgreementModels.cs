using System;
using System.IO;

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
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal AgreedLeaveDays { get; set; }
        public bool IsSigned { get; set; }
        public bool HasAgreementPdf { get; set; }
        public string AgreementPdfFileName { get; set; } = string.Empty;
        public string AgreementPdfOriginalFileName { get; set; } = string.Empty;
        public string AgreementPdfContentType { get; set; } = string.Empty;
        public DateTime? CreatedDate { get; set; }
    }

    public class AdminLeaveAgreementUpdateModel
    {
        public int Id { get; set; }
        public decimal AgreedLeaveDays { get; set; }
        public bool IsSigned { get; set; }
    }

    public class AdminLeaveAgreementPdfUpdateModel
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
    }

    public class LeaveAgreementUploadRequestModel
    {
        public Stream ExcelStream { get; set; } = Stream.Null;
        public string CurrentUser { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
    }
}
