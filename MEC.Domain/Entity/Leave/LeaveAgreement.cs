using MEC.Domain.Common;
using MEC.Domain.Entity.Employee;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.Leave
{
    [Table("leave_agreement")]
    public class LeaveAgreement : BaseEntity
    {
        [Column("employee_portal_id")]
        public int EmployeePortalId { get; set; }

        [ForeignKey(nameof(EmployeePortalId))]
        public EmployeePortal? EmployeePortal { get; set; }

        [Column("agreed_leave_days", TypeName = "decimal(10,2)")]
        public decimal AgreedLeaveDays { get; set; }

        [Column("balance_as_of_date")]
        public DateTime? BalanceAsOfDate { get; set; }

        [Column("current_year_earned_days", TypeName = "decimal(10,2)")]
        public decimal CurrentYearEarnedDays { get; set; }

        [Column("current_year_used_days", TypeName = "decimal(10,2)")]
        public decimal CurrentYearUsedDays { get; set; }

        [Column("is_signed")]
        public bool IsSigned { get; set; }

        [Column("agreement_pdf_file_name")]
        public string? AgreementPdfFileName { get; set; }

        [Column("agreement_pdf_original_file_name")]
        public string? AgreementPdfOriginalFileName { get; set; }

        [Column("agreement_pdf_content_type")]
        public string? AgreementPdfContentType { get; set; }

        [Column("agreement_pdf_size_bytes")]
        public long? AgreementPdfSizeBytes { get; set; }

        [Column("agreement_pdf_uploaded_at")]
        public DateTime? AgreementPdfUploadedAt { get; set; }
    }
}
