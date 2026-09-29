using System.Text.Json;
using MEC.Application.Abstractions.Service.LeaveService.Model;
using MEC.Domain.Common;
using MEC.Domain.Entity.Employee;
using MEC.Domain.Entity.Leave;
using Microsoft.EntityFrameworkCore;

namespace MEC.Application.Service.LeaveService;

public sealed partial class LeaveAccountingService
{
    public async Task<bool> SetStatusAsync(int leaveId, int status, string actor)
    {

        var id = await db.Leaves.Where(x => x.Id == leaveId).Select(x => (int?)x.EmployeeId).SingleOrDefaultAsync();
        if (!id.HasValue) return false;
        return await WithEmployeeAsync(id.Value, async employee =>
        {
            var leave = await db.Leaves.Include(x => x.LeaveType).SingleAsync(x => x.Id == leaveId);
            if (leave.Status == status) return true;
            if (leave.Status != 0 && leave.Status != 4) throw new InvalidOperationException("İzin daha önce sonuçlandırılmış.");
            if (status != 1 && status != 2 && status != 3) throw new InvalidOperationException("Geçersiz karar.");
            await EnsureAccountAsync(employee);
            if (status == 1)
            {
                var error = await ValidateDatesAsync(employee, leave.StartDate, leave.EndDate, leave.Id);
                if (error != null) throw new InvalidOperationException(error);
                if (leave.StartDate > LeaveAccountingRules.Now)
                {
                    leave.RequestedDays = await CalculateDaysAsync(leave.StartDate, leave.EndDate);
                    await CaptureCalculationAsync(leave);
                }
                var limit = leave.LeaveType?.Code?.ToUpperInvariant() switch { LeaveTypeCodes.Marriage or LeaveTypeCodes.Bereavement => 3m, LeaveTypeCodes.Paternity => 5m, _ => 0m };
                if (limit > 0 && leave.RequestedDays > limit) throw new InvalidOperationException($"Bu izin türünde talep başına en fazla {limit} gün kullanılabilir.");
                if (leave.RequestedDays <= 0) throw new InvalidOperationException("İzin süresi sıfır. Tarihleri kontrol edin.");
                await ChargeAsync(leave, employee, true, actor);
            }
            leave.Status = status; leave.DecisionBy = actor; leave.DecisionDate = DateTime.UtcNow;
            leave.UpdateDate = DateTime.UtcNow;
            return true;
        });
    }

    public async Task SaveAgreementAsync(int employeeId, decimal balance, DateTime cutoff, decimal earned,
        decimal used, bool signed, string actor, DateTime? correctedHire = null, DateTime? correctedBirth = null,
        string? reason = null, bool reactivate = false)
    {
        await RequireAdminAsync(actor);
        await WithEmployeeAsync(employeeId, async employee =>
        {
            if (cutoff.Date > Today || cutoff.Year <= 1900 || earned < 0 || used < 0)
                throw new InvalidOperationException("Mutabakat tarihi ve bilgi alanlarını kontrol edin.");
            var agreement = await db.LeaveAgreements.SingleOrDefaultAsync(x => x.EmployeePortalId == employeeId);
            if (correctedHire.HasValue)
            {
                ValidatePersonalDates(correctedHire.Value, correctedBirth);
                if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Tarih düzeltme gerekçesi gerekiyor.");
                if (reactivate && !employee.IsDeleted) throw new InvalidOperationException("Personel zaten aktif.");
                if (reactivate && (cutoff.Date < correctedHire.Value.Date ||
                    (employee.TerminationDate.HasValue && correctedHire.Value.Date <= employee.TerminationDate.Value.Date)))
                    throw new InvalidOperationException("Yeni giriş önceki çıkıştan sonra, mutabakat tarihi yeni girişten önce olmayacak şekilde girilmelidir.");
                await EnsureAccountAsync(employee);
            }
            var changed = agreement == null || agreement.AgreedLeaveDays != balance ||
                agreement.BalanceAsOfDate?.Date != cutoff.Date || agreement.CurrentYearEarnedDays != earned ||
                agreement.CurrentYearUsedDays != used || correctedHire.HasValue;
            if (agreement == null)
            {
                agreement = new LeaveAgreement { EmployeePortalId = employeeId, CreatedDate = DateTime.UtcNow };
                db.LeaveAgreements.Add(agreement);
            }
            else if (changed)
            {
                await ArchiveAgreementAsync(agreement, actor);
                agreement.AgreementPdfFileName = null; agreement.AgreementPdfOriginalFileName = null;
                agreement.AgreementPdfContentType = null; agreement.AgreementPdfSizeBytes = null;
                agreement.AgreementPdfUploadedAt = null;
            }
            if (signed && !changed && string.IsNullOrWhiteSpace(agreement.AgreementPdfFileName))
                throw new InvalidOperationException("İmzalı olarak işaretlemeden önce bu sürümün PDF'ini yükleyin.");
            agreement.IsSigned = !changed && signed;
            agreement.AgreedLeaveDays = balance; agreement.BalanceAsOfDate = cutoff.Date;
            agreement.CurrentYearEarnedDays = earned; agreement.CurrentYearUsedDays = used;
            agreement.UpdateDate = DateTime.UtcNow;
            if (correctedHire.HasValue)
            {
                reason = $"Giriş: {employee.HireDate:yyyy-MM-dd} → {correctedHire:yyyy-MM-dd}; doğum: {employee.BirthDate:yyyy-MM-dd} → {correctedBirth:yyyy-MM-dd}. " + reason;
                employee.HireDate = correctedHire.Value.Date; employee.BirthDate = correctedBirth?.Date;
                if (reactivate) { employee.TerminationDate = null; employee.IsDeleted = false; }
            }
            if (changed)
            {
                await ApplyAgreementAsync(employee, agreement, actor);
                if (correctedHire.HasValue)
                    await MoveAsync(employee, await EnsureAccountAsync(employee), 0, "DateCorrection", "date:" + Guid.NewGuid(), actor, reason!, Today);
            }
            return true;
        });
    }

    public async Task UploadAgreementPdfAsync(AdminLeaveAgreementPdfUpdateModel model)
    {
        await RequireAdminAsync(model.CurrentUser);
        var id = await db.LeaveAgreements.Where(x => x.Id == model.Id).Select(x => x.EmployeePortalId).SingleAsync();
        await WithEmployeeAsync(id, async _ =>
        {
            var agreement = await db.LeaveAgreements.SingleAsync(x => x.Id == model.Id);
            if (!string.IsNullOrWhiteSpace(agreement.AgreementPdfFileName)) await ArchiveAgreementAsync(agreement, model.CurrentUser);
            agreement.AgreementPdfFileName = model.FileName;
            agreement.AgreementPdfOriginalFileName = model.OriginalFileName;
            agreement.AgreementPdfContentType = "application/pdf";
            agreement.AgreementPdfSizeBytes = model.SizeBytes;
            agreement.AgreementPdfUploadedAt = DateTime.UtcNow;
            agreement.IsSigned = false;
            return true;
        });
    }

    public async Task ValidateProfileChangeAsync(EmployeePortal employee, DateTime? hire, DateTime? birth,
        DateTime? termination, bool deleted, string actor)
    {
        await RequireAdminAsync(actor);
        var account = await EnsureAccountAsync(employee);
        if (employee.HireDate?.Date != hire?.Date || employee.BirthDate?.Date != birth?.Date)
            throw new InvalidOperationException("İşe giriş ve doğum tarihini İzin hesabı ekranından önizleme veya yeni mutabakatla düzeltin.");
        if (employee.IsDeleted && !deleted)
            throw new InvalidOperationException("Yeniden işe giriş için İzin hesabı ekranında yeni giriş tarihi ve mutabakat girin.");
        if (deleted && !employee.IsDeleted)
        {
            if (!termination.HasValue || termination.Value.Date > Today || !hire.HasValue || termination.Value.Date < hire.Value.Date)
                throw new InvalidOperationException("Pasife almak için işe girişten önce olmayan ve bugünü aşmayan çıkış tarihi gerekiyor.");
            if (LeaveAccountingRules.CompletedYears(hire, termination.Value.Date) < account.CoveredServiceYears)
                throw new InvalidOperationException("Çıkış tarihi açılışa dahil hakları etkiliyor. Önce çıkış tarihli mutabakat yapın.");
            var credits = await db.LeaveAccruals.Where(x=>x.EmployeeId==employee.Id && x.Cycle==account.Cycle && x.Anniversary>termination.Value.Date).ToListAsync();
            foreach(var credit in credits)
            {
                await MoveAsync(employee,account,-credit.CreditedDays,"TerminationCorrection","exit:"+Guid.NewGuid(),actor,"Çıkış tarihinden sonraki hak ediş geri alındı.",termination.Value.Date,serviceYear:credit.ServiceYear);
                credit.CreditedDays=0;
            }
            employee.TerminationDate = termination.Value.Date;
            await AccrueAsync(employee, account, termination.Value.Date, actor);
            account.ProcessedThrough=termination.Value.Date;employee.AnnualLeaveProcessedThrough=termination.Value.Date;
        }
        else if (termination?.Date != employee.TerminationDate?.Date)
            throw new InvalidOperationException("Çıkış tarihi yalnız pasife alma sırasında değiştirilebilir.");
    }

    public async Task<AccountPage> GetAccountPageAsync(int id, string actor)
    {
        await RequireAdminAsync(actor);
        await WithEmployeeAsync(id, async employee => { await EnsureAccountAsync(employee); return true; });
        return new AccountPage
        {
            Employee = await db.EmployeePortals.AsNoTracking().SingleAsync(x => x.Id == id),
            Account = await db.LeaveAccounts.AsNoTracking().SingleAsync(x => x.EmployeeId == id),
            Movements = await db.LeaveMovements.AsNoTracking().Where(x => x.EmployeeId == id).OrderByDescending(x => x.Id).Take(200).ToListAsync(),
            Versions = await db.LeaveAgreementVersions.AsNoTracking().Where(x => x.EmployeeId == id).OrderByDescending(x => x.Id).ToListAsync()
        };
    }

    public async Task ExpireCancellationsAsync()
    {
        db.ChangeTracker.Clear();
        await using var tx=await db.Database.BeginTransactionAsync();
        await LockPolicyAsync();
        var now=LeaveAccountingRules.Now;
        await db.LeaveCancellations.Where(x=>x.Status=="Pending" &&
            db.Leaves.Any(l=>l.Id==x.LeaveId && l.StartDate<=now))
            .ExecuteUpdateAsync(setters=>setters.SetProperty(x=>x.Status,"Expired").SetProperty(x=>x.DecisionDate,DateTime.UtcNow));
        await tx.CommitAsync();
    }
    public async Task SaveImportBatchAsync(string hash, string input, string? result, string actor)
    {
        await RequireAdminAsync(actor);
        db.ChangeTracker.Clear();
        await using var tx=await db.Database.BeginTransactionAsync();
        await LockPolicyAsync();
        var batch=await db.LeaveImportBatches.SingleOrDefaultAsync(x=>x.BatchHash==hash);
        if(batch==null){batch=new LeaveImportBatch{BatchHash=hash,InputJson=input,Actor=actor,CreatedDate=DateTime.UtcNow};db.LeaveImportBatches.Add(batch);}
        if(result!=null) batch.ResultJson=result;
        batch.UpdateDate=DateTime.UtcNow;
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task<List<LeaveImportBatch>> GetImportsAsync(string actor)
    {
        await RequireAdminAsync(actor);
        return await db.LeaveImportBatches.AsNoTracking().OrderByDescending(x=>x.UpdateDate).Take(100).ToListAsync();
    }
    public async Task<List<LeaveJobResult>> GetJobResultsAsync(string actor)
    {
        await RequireAdminAsync(actor);
        return await db.LeaveJobResults.AsNoTracking().OrderByDescending(x => x.Id).Take(300).ToListAsync();
    }

    public async Task<DecisionDetails> GetDecisionDetailsAsync(int leaveId, string actor)
    {
        var leave = await db.Leaves.AsNoTracking().Include(x => x.LeaveType).SingleAsync(x => x.Id == leaveId);
        var person = await workflow.GetActorAsync(actor);
        var route = await workflow.ResolveRouteAsync(leave.EmployeeId);
        if (person == null || (!person.IsAdministrator && !person.IsFinalApprover &&
            person.EmployeePortalId != leave.EmployeeId && !(route?.ManagerApprovers.Any(x => x.Email.Equals(actor, StringComparison.OrdinalIgnoreCase)) ?? false)))
            throw new UnauthorizedAccessException();
        var cancellation = await db.LeaveCancellations.AsNoTracking().SingleOrDefaultAsync(x => x.LeaveId == leaveId);
        var special = leave.LeaveType?.Code is LeaveTypeCodes.Marriage or LeaveTypeCodes.Bereavement or LeaveTypeCodes.Paternity;
        return new DecisionDetails
        {
            Leave = leave, Cancellation = cancellation,
            CanRequestCancellation = person.EmployeePortalId == leave.EmployeeId && leave.Status == 1 &&
                leave.StartDate > LeaveAccountingRules.Now && cancellation == null,
            CanDecideCancellation = (person.IsAdministrator || person.IsFinalApprover) && cancellation?.Status == "Pending" &&
                leave.Status == 1 && leave.StartDate > LeaveAccountingRules.Now,
            CanReviewSplit = person.IsAdministrator,
            CanReviewHistorical = person.IsAdministrator && (leave.Status == 0 || leave.Status == 4),
            Previous = special ? await db.Leaves.AsNoTracking().Where(x => x.EmployeeId == leave.EmployeeId &&
                x.Id != leave.Id && x.LeaveTypeId == leave.LeaveTypeId).OrderByDescending(x => x.CreatedDate).Take(30).ToListAsync() : []
        };
    }
}

public class AccountPage
{
    public EmployeePortal Employee { get; set; } = null!;
    public LeaveAccount Account { get; set; } = null!;
    public List<LeaveMovement> Movements { get; set; } = [];
    public List<LeaveAgreementVersion> Versions { get; set; } = [];
    public DateCorrectionPreview? Preview { get; set; }
}
public class DecisionDetails
{
    public Leave Leave { get; set; } = null!;
    public LeaveCancellation? Cancellation { get; set; }
    public bool CanRequestCancellation { get; set; }
    public bool CanDecideCancellation { get; set; }
    public bool CanReviewHistorical { get; set; }
    public bool CanReviewSplit { get; set; }
    public List<Leave> Previous { get; set; } = [];
}
