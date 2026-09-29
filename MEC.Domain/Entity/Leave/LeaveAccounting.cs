using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MEC.Domain.Common;

namespace MEC.Domain.Entity.Leave;

[Table("leave_account")]
public class LeaveAccount : BaseEntity
{
    public int EmployeeId { get; set; }
    public int Cycle { get; set; } = 1;
    public long Revision { get; set; }
    public DateTime OpeningDate { get; set; }
    public DateTime ProcessedThrough { get; set; }
    public DateTime? HireDate { get; set; }
    public DateTime? BirthDate { get; set; }
    public int CoveredServiceYears { get; set; }
    public bool NeedsReview { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal Balance { get; set; }
}

[Table("leave_movement")]
public class LeaveMovement : BaseEntity
{
    public int EmployeeId { get; set; }
    public int Cycle { get; set; }
    public string OperationKey { get; set; } = "";
    public string Kind { get; set; } = "";
    public DateTime EffectiveDate { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal Days { get; set; }
    public int? LeaveId { get; set; }
    public int? ServiceYear { get; set; }
    public string Actor { get; set; } = "";
    [MaxLength(2000)] public string Reason { get; set; } = "";
}

[Table("leave_accrual")]
public class LeaveAccrual : BaseEntity
{
    public int EmployeeId { get; set; }
    public int Cycle { get; set; }
    public int ServiceYear { get; set; }
    public DateTime Anniversary { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal CreditedDays { get; set; }
}

[Table("leave_charge")]
public class LeaveCharge : BaseEntity
{
    [Column(TypeName = "longtext")] public string CalculationJson { get; set; } = "";
    public DateTime? SplitCutoff { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal? SplitBeforeDays { get; set; }
    public int LeaveId { get; set; }
    public int EmployeeId { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal Days { get; set; }
    public bool IncludedInOpening { get; set; }
    public bool? HistoricalIncluded { get; set; }
    public string ReviewReason { get; set; } = "";
}

[Table("leave_agreement_version")]
public class LeaveAgreementVersion : BaseEntity
{
    public int AgreementId { get; set; }
    public int EmployeeId { get; set; }
    public int Version { get; set; }
    public string Actor { get; set; } = "";
    [Column(TypeName = "longtext")] public string Snapshot { get; set; } = "";
}

[Table("leave_cancellation")]
public class LeaveCancellation : BaseEntity
{
    public int LeaveId { get; set; }
    public string Status { get; set; } = "Pending";
    [MaxLength(2000)] public string Reason { get; set; } = "";
    public string RequestedBy { get; set; } = "";
    public string DecidedBy { get; set; } = "";
    public DateTime? DecisionDate { get; set; }
}

[Table("leave_import_row")]
public class LeaveImportRow : BaseEntity
{
    public string BatchHash { get; set; } = "";
    public string Email { get; set; } = "";
    public int EmployeeId { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal Days { get; set; }
    public string Actor { get; set; } = "";
}

[Table("leave_job_result")]
public class LeaveJobResult : BaseEntity
{
    public string RunId { get; set; } = "";
    public int EmployeeId { get; set; }
    public DateTime BusinessDate { get; set; }
    public bool Success { get; set; }
    [MaxLength(2000)] public string Error { get; set; } = "";
}

[Table("leave_calendar")]
public class LeaveCalendar : BaseEntity
{
    public int Year { get; set; }
    public long Revision { get; set; }
    public bool IsApproved { get; set; }
    public string ApprovedBy { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    [Column(TypeName = "longtext")] public string DraftJson { get; set; } = "[]";
    [Column(TypeName = "longtext")] public string PublishedJson { get; set; } = "[]";
}

[Table("leave_calendar_version")]
public class LeaveCalendarVersion : BaseEntity
{
    public int Year { get; set; }
    public long Revision { get; set; }
    public string Actor { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    [Column(TypeName = "longtext")] public string HolidaysJson { get; set; } = "[]";
}

[Table("leave_import_batch")]
public class LeaveImportBatch : BaseEntity
{
    [MaxLength(255)] public string BatchHash { get; set; } = "";
    [MaxLength(255)] public string Actor { get; set; } = "";
    [Column(TypeName="longtext")] public string InputJson { get; set; } = "[]";
    [Column(TypeName="longtext")] public string ResultJson { get; set; } = "";
}
