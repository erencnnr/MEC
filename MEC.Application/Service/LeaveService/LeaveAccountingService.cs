using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MEC.Application.Abstractions.Service.ApprovalWorkflowService;
using MEC.DAL.Config.Contexts;
using MEC.Domain.Common;
using MEC.Domain.Common.Enum;
using MEC.Domain.Entity.Employee;
using MEC.Domain.Entity.Leave;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MEC.Application.Service.LeaveService;

public sealed partial class LeaveAccountingService(ApplicationDbContext db, IApprovalWorkflowService workflow, Microsoft.Extensions.Logging.ILogger<LeaveAccountingService>? logger = null)
{
    private readonly List<Func<Task>> afterCommit = [];
    public async Task NotifyAsync(Func<Task> notification)
    {
        if (InTransaction) { afterCommit.Add(notification); return; }
        try { await notification(); }
        catch (Exception ex) { logger?.LogError(ex, "İzin işlemi tamamlandı fakat bildirim gönderilemedi."); }
    }
    public bool InTransaction => db.Database.CurrentTransaction != null;
    public static DateTime Today => LeaveAccountingRules.Now.Date;

    public async Task RequireAdminAsync(string actor)
    {
        if ((await workflow.GetActorAsync(actor))?.IsAdministrator != true)
            throw new UnauthorizedAccessException("Bu işlem yalnız Admin yetkisine açıktır.");
    }

    // All leave writers acquire the policy gate first, then the employee row. This also
    // serializes policy publication with approvals, imports and previews being applied.
    public async Task<T> WithEmployeeAsync<T>(int id, Func<EmployeePortal, Task<T>> action)
    {
        if (InTransaction)
            return await action(await db.EmployeePortals.SingleAsync(x => x.Id == id));
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            await LockPolicyAsync();
            var employee = (await db.EmployeePortals.FromSqlInterpolated(
                $"SELECT * FROM employee_portal WHERE Id = {id} FOR UPDATE").ToListAsync()).Single();
            var result = await action(employee);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            await tx.DisposeAsync();
            var notifications = afterCommit.ToArray(); afterCommit.Clear();
            foreach (var notification in notifications) await NotifyAsync(notification);
            return result;
        }
        catch { afterCommit.Clear(); await tx.RollbackAsync(); db.ChangeTracker.Clear(); throw; }
    }

    public async Task LockPolicyAsync()
    {
        var gate = await db.LeaveCalendars.FromSqlRaw("SELECT * FROM leave_calendar WHERE Year = 0 FOR UPDATE").ToListAsync();
        if (gate.Count != 1) throw new InvalidOperationException("İzin muhasebesi veri geçişi uygulanmamış.");
    }

    public async Task<LeaveAccount> EnsureAccountAsync(EmployeePortal employee)
    {
        var account = await db.LeaveAccounts.SingleOrDefaultAsync(x => x.EmployeeId == employee.Id);
        if (account != null) return account;
        var through = employee.AnnualLeaveProcessedThrough?.Date ?? Today;
        account = new LeaveAccount
        {
            EmployeeId = employee.Id, OpeningDate = Today, ProcessedThrough = through,
            HireDate = employee.HireDate?.Date, BirthDate = employee.BirthDate?.Date,
            CoveredServiceYears = LeaveAccountingRules.CompletedYears(employee.HireDate, through),
            NeedsReview = !employee.HireDate.HasValue ||
                (!employee.AnnualLeaveProcessedThrough.HasValue && LeaveAccountingRules.CompletedYears(employee.HireDate, Today) > 0),
            CreatedDate = DateTime.UtcNow
        };
        db.LeaveAccounts.Add(account);
        await MoveAsync(employee, account, employee.LeaveDays, "Opening", "opening", "migration",
            "Mevcut bakiye korunarak açılış yapıldı.", Today);
        var existing = await db.Leaves.Include(x => x.LeaveType)
            .Where(x => x.EmployeeId == employee.Id && x.Status == (int)LeaveStatus.Approved).ToListAsync();
        foreach (var leave in existing.Where(IsAnnual))
        {
            var charge = await db.LeaveCharges.SingleOrDefaultAsync(x=>x.LeaveId==leave.Id);
            if (charge == null) { charge = new LeaveCharge { EmployeeId=employee.Id, LeaveId=leave.Id, CreatedDate=DateTime.UtcNow }; db.LeaveCharges.Add(charge); }
            charge.Days=leave.RequestedDays; charge.IncludedInOpening=true; charge.HistoricalIncluded=true;
        }
        await db.SaveChangesAsync();
        return account;
    }

    public async Task<bool> MoveAsync(EmployeePortal employee, LeaveAccount account, decimal days, string kind,
        string key, string actor, string reason, DateTime effective, int? leaveId = null, int? serviceYear = null)
    {
        if (await db.LeaveMovements.AnyAsync(x => x.EmployeeId == employee.Id && x.OperationKey == key)) return false;
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 2000) throw new InvalidOperationException("İşlem gerekçesi zorunludur ve 2000 karakteri aşamaz.");
        if (Math.Abs(account.Balance + days) > 99999999.99m || decimal.Round(days, 2) != days)
            throw new InvalidOperationException("İzin günü tutarı geçersiz.");
        db.LeaveMovements.Add(new LeaveMovement { EmployeeId = employee.Id, Cycle = account.Cycle,
            Days = days, Kind = kind, OperationKey = key, Actor = actor, Reason = reason,
            EffectiveDate = effective, LeaveId = leaveId, ServiceYear = serviceYear, CreatedDate = DateTime.UtcNow });
        account.Balance += days;
        account.Revision++;
        account.UpdateDate = DateTime.UtcNow;
        employee.LeaveDays = account.Balance;
        employee.UpdateDate = DateTime.UtcNow;
        return true;
    }

    public async Task<decimal> RefreshAsync(int id, DateTime through)
        => await WithEmployeeAsync(id, async employee =>
        {
            var account = await EnsureAccountAsync(employee);
            if (!employee.IsDeleted && !account.NeedsReview) await AccrueAsync(employee, account, through, "annual-job");
            return account.Balance;
        });

    public async Task AccrueAsync(EmployeePortal employee, LeaveAccount account, DateTime through, string actor)
    {
        if (account.NeedsReview) throw new InvalidOperationException("Önce personel için açılış mutabakatını tamamlayın.");
        if (account.HireDate != employee.HireDate?.Date || account.BirthDate != employee.BirthDate?.Date)
            throw new InvalidOperationException("Personel tarihleri değişmiş. Kontrollü tarih düzeltmesi veya mutabakat gerekiyor.");
        if (!employee.HireDate.HasValue || employee.HireDate.Value.Year <= 1900)
            throw new InvalidOperationException("Geçerli işe giriş tarihi gerekiyor.");
        var end = employee.TerminationDate?.Date < through.Date ? employee.TerminationDate.Value.Date : through.Date;
        var completed = LeaveAccountingRules.CompletedYears(employee.HireDate, end);
        var existing = await db.LeaveAccruals.Where(x => x.EmployeeId == employee.Id && x.Cycle == account.Cycle).ToListAsync();
        for (var year = account.CoveredServiceYears + 1; year <= completed; year++)
        {
            var date = employee.HireDate.Value.Date.AddYears(year);
            var days = AnnualLeaveEntitlementCalculator.CalculateEntitlement(employee.HireDate.Value, employee.BirthDate, date);
            var credit = existing.SingleOrDefault(x => x.ServiceYear == year);
            if (credit == null)
            {
                credit = new LeaveAccrual { EmployeeId = employee.Id, Cycle = account.Cycle, ServiceYear = year, Anniversary = date };
                db.LeaveAccruals.Add(credit); existing.Add(credit);
            }
            var delta = days - credit.CreditedDays;
            if (delta != 0)
            {
                await MoveAsync(employee, account, delta, "AnnualAccrual", $"accrual:{account.Cycle}:{year}:{account.Revision}", actor,
                    $"{year}. çalışma yılı hak edişi", date, serviceYear: year);
                credit.CreditedDays = days;
                credit.Anniversary = date;
            }
        }
        account.ProcessedThrough = end > account.ProcessedThrough ? end : account.ProcessedThrough;
        employee.AnnualLeaveProcessedThrough = account.ProcessedThrough;
    }

    public async Task ManualAsync(int id, decimal days, string reason, string actor, string operationId)
    {
        await RequireAdminAsync(actor);
        if (days == 0 || !Guid.TryParse(operationId, out _)) throw new InvalidOperationException("Geçerli işlem ve gün sayısı gerekiyor.");
        await WithEmployeeAsync(id, async employee =>
        {
            var account = await EnsureAccountAsync(employee);
            await MoveAsync(employee, account, days, "Manual", "manual:" + operationId, actor, reason, Today);
            return true;
        });
    }

    public async Task<bool> ImportRowAsync(int id, decimal days, string reason, string actor, string batchHash, string email)
    {
        await RequireAdminAsync(actor);
        if (days <= 0) throw new InvalidOperationException("Excel ile eklenecek gün sayısı sıfırdan büyük olmalıdır.");
        return await WithEmployeeAsync(id, async employee =>
        {
            if (await db.LeaveImportRows.AnyAsync(x => x.BatchHash == batchHash && x.Email == email)) return false;
            if (employee.IsDeleted) throw new InvalidOperationException("Personel pasif.");
            var account = await EnsureAccountAsync(employee);
            await MoveAsync(employee, account, days, "Excel", "excel:" + batchHash + ":" + id, actor,
                string.IsNullOrWhiteSpace(reason) ? "Excel ile izin ekleme" : reason, Today);
            db.LeaveImportRows.Add(new LeaveImportRow { BatchHash = batchHash, Email = email, EmployeeId = id,
                Days = days, Actor = actor, CreatedDate = DateTime.UtcNow });
            return true;
        });
    }

    public async Task<string?> ValidateDatesAsync(EmployeePortal employee, DateTime start, DateTime end, int? excludeId = null)
    {
        if (employee.IsDeleted) return "Personel pasif.";
        if (!employee.HireDate.HasValue || employee.HireDate.Value.Year <= 1900) return "Önce admin işe giriş tarihini tamamlamalıdır.";
        if (start.Date < employee.HireDate.Value.Date || (employee.TerminationDate.HasValue && end.Date > employee.TerminationDate.Value.Date))
            return "İzin tarihleri çalışma dönemi içinde olmalıdır.";
        if (await db.Leaves.AnyAsync(x => x.EmployeeId == employee.Id && x.Id != excludeId &&
            (x.Status == 0 || x.Status == 1 || x.Status == 4) && x.StartDate < end && x.EndDate > start))
            return "Bu tarih ve saatlerde bekleyen veya onaylı başka bir izin bulunuyor.";
        return await CalendarErrorAsync(start, end);
    }

    public async Task<string?> CalendarErrorAsync(DateTime start, DateTime end)
    {
        if (start > end) return "Tarih aralığı geçersiz.";
        var years = await db.LeaveCalendars.Where(x => x.Year >= start.Year && x.Year <= end.Year && x.IsApproved)
            .Select(x => x.Year).ToListAsync();
        var missing = Enumerable.Range(start.Year, end.Year - start.Year + 1).Except(years).ToList();
        return missing.Count == 0 ? null : $"{string.Join(", ", missing)} tatil takvimi admin tarafından kullanıma açılmalıdır.";
    }

    public async Task<decimal> CalculateDaysAsync(DateTime start, DateTime end)
    {
        var calendars = await db.LeaveCalendars.Where(x => x.Year >= start.Year && x.Year <= end.Year && x.IsApproved).ToListAsync();
        var holidays = calendars.SelectMany(x => JsonSerializer.Deserialize<List<CalendarDay>>(x.PublishedJson) ?? [])
            .Select(x => new HolidayInterval(x.Start, x.End)).ToList();
        var policies = await db.LeavePolicySettings.OrderBy(x => x.EffectiveFrom).ToListAsync();
        return LeaveDurationCalculator.CalculateRequestedDays(start, end, holidays,
            policies.Select(x => new SaturdayLeavePolicy(x.EffectiveFrom, x.CountSaturday)));
    }

    public async Task ChargeAsync(Leave leave, EmployeePortal employee, bool approve, string actor)
    {
        if (!IsAnnual(leave)) return;
        var account = await EnsureAccountAsync(employee);
        var charge = await db.LeaveCharges.SingleOrDefaultAsync(x => x.LeaveId == leave.Id);
        if (charge == null)
        {
            charge = new LeaveCharge { EmployeeId = employee.Id, LeaveId = leave.Id };
            db.LeaveCharges.Add(charge);
        }
        var target = approve ? leave.RequestedDays : 0m;
        var includedBefore = 0m;
        if (approve && leave.StartDate.Date <= account.OpeningDate && !charge.HistoricalIncluded.HasValue)
            throw new InvalidOperationException("Mutabakat öncesindeki izin için adminin dahil/değil incelemesi gerekiyor.");
        if (approve && charge.HistoricalIncluded == true && !charge.IncludedInOpening)
        {
            var before = await BeforeCutoffAsync(leave, account.OpeningDate);
            target = LeaveAccountingRules.DaysAfterCutoff(leave.RequestedDays, before);
            includedBefore = leave.RequestedDays - target;
        }
        var delta = charge.Days - target;
        if (delta != 0)
            await MoveAsync(employee, account, delta, approve ? "LeaveUse" : "LeaveRefund",
                $"leave:{leave.Id}:{account.Revision}", actor, approve ? "Yıllık izin onayı" : "Onaylı izin iptali",
                Today, leave.Id);
        // An explicitly confirmed opening deduction is also refundable, although it is not debited again here.
        charge.Days = target + includedBefore;
        leave.RemainingLeaveDays = account.Balance;
    }

    public async Task ReviewHistoricalAsync(int leaveId, bool included, string reason, string actor)
    {
        await RequireAdminAsync(actor);
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("İnceleme gerekçesi gerekiyor.");
        var id = await db.Leaves.Where(x => x.Id == leaveId).Select(x => x.EmployeeId).SingleAsync();
        await WithEmployeeAsync(id, async employee =>
        {
            var leave = await db.Leaves.SingleAsync(x => x.Id == leaveId);
            if (leave.Status != 0 && leave.Status != 4) throw new InvalidOperationException("Talep artık beklemiyor.");
            var charge = await db.LeaveCharges.SingleOrDefaultAsync(x => x.LeaveId == leaveId);
            if (charge == null) { charge = new LeaveCharge { EmployeeId = id, LeaveId = leaveId }; db.LeaveCharges.Add(charge); }
            charge.HistoricalIncluded = included; charge.ReviewReason = actor + ": " + reason;
            return true;
        });
    }

    public async Task RequestCancellationAsync(int leaveId, string reason, string actor)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("İptal gerekçesi gerekiyor.");
        var id = await db.Leaves.Where(x => x.Id == leaveId).Select(x => x.EmployeeId).SingleAsync();
        await WithEmployeeAsync(id, async employee =>
        {
            var leave = await db.Leaves.SingleAsync(x => x.Id == leaveId);
            if (!string.Equals(employee.Email, actor, StringComparison.OrdinalIgnoreCase) || employee.IsDeleted)
                throw new UnauthorizedAccessException();
            if (leave.Status != 1 || leave.StartDate <= LeaveAccountingRules.Now)
                throw new InvalidOperationException("Yalnız başlamamış onaylı izin için iptal istenebilir.");
            var request = await db.LeaveCancellations.SingleOrDefaultAsync(x => x.LeaveId == leaveId);
            if (request != null) throw new InvalidOperationException("Bu izin için iptal talebi zaten var.");
            db.LeaveCancellations.Add(new LeaveCancellation { LeaveId = leaveId, Reason = reason,
                RequestedBy = actor, CreatedDate = DateTime.UtcNow });
            return true;
        });
    }

    public async Task<bool> DecideCancellationAsync(int leaveId, bool approve, string actor)
    {
        var who = await workflow.GetActorAsync(actor);
        if (who == null || (!who.IsAdministrator && !who.IsFinalApprover)) throw new UnauthorizedAccessException();
        var id = await db.Leaves.Where(x => x.Id == leaveId).Select(x => x.EmployeeId).SingleAsync();
        return await WithEmployeeAsync(id, async employee =>
        {
            var request = await db.LeaveCancellations.SingleAsync(x => x.LeaveId == leaveId);
            var leave = await db.Leaves.Include(x => x.LeaveType).SingleAsync(x => x.Id == leaveId);
            if (request.Status != "Pending") return false;
            request.DecidedBy = actor; request.DecisionDate = DateTime.UtcNow;
            if (leave.StartDate <= LeaveAccountingRules.Now || leave.Status != 1)
            { request.Status = "Expired"; return false; }
            request.Status = approve ? "Approved" : "Rejected";
            if (approve)
            {
                // Initialize while the original approval is still visible, so a legacy charge can be refunded.
                await EnsureAccountAsync(employee);
                await ChargeAsync(leave, employee, false, actor);
                leave.Status = 3; leave.DecisionBy = actor; leave.DecisionDate = DateTime.UtcNow;
            }
            return approve;
        });
    }

    public async Task ArchiveAgreementAsync(LeaveAgreement agreement, string actor)
    {
        var version = (await db.LeaveAgreementVersions.Where(x => x.AgreementId == agreement.Id)
            .MaxAsync(x => (int?)x.Version) ?? 0) + 1;
        // Exclude navigation properties and retain the old PDF metadata with the version.
        var snapshot = JsonSerializer.Serialize(new { agreement.AgreedLeaveDays, agreement.BalanceAsOfDate,
            agreement.CurrentYearEarnedDays, agreement.CurrentYearUsedDays, agreement.IsSigned,
            agreement.AgreementPdfFileName, agreement.AgreementPdfOriginalFileName,
            agreement.AgreementPdfContentType, agreement.AgreementPdfSizeBytes, agreement.AgreementPdfUploadedAt });
        db.LeaveAgreementVersions.Add(new LeaveAgreementVersion { AgreementId = agreement.Id,
            EmployeeId = agreement.EmployeePortalId, Version = version, Actor = actor,
            Snapshot = snapshot, CreatedDate = DateTime.UtcNow });
    }

    public async Task ApplyAgreementAsync(EmployeePortal employee, LeaveAgreement agreement, string actor)
    {
        if (!agreement.BalanceAsOfDate.HasValue || agreement.BalanceAsOfDate.Value.Date > Today)
            throw new InvalidOperationException("Geçerli mutabakat tarihi gerekiyor.");
        var account = await EnsureAccountAsync(employee);
        var cutoff = agreement.BalanceAsOfDate.Value.Date;
        if (!employee.HireDate.HasValue || employee.HireDate.Value.Year <= 1900)
            throw new InvalidOperationException("Önce işe giriş tarihini tamamlayın.");
        account.Cycle++; account.OpeningDate = cutoff; account.ProcessedThrough = cutoff;
        account.HireDate = employee.HireDate.Value.Date; account.BirthDate = employee.BirthDate?.Date;
        account.CoveredServiceYears = LeaveAccountingRules.CompletedYears(employee.HireDate, cutoff);
        account.NeedsReview = false;
        var manualAfter = await db.LeaveMovements.Where(x => x.EmployeeId == employee.Id &&
            (x.Kind == "Manual" || x.Kind == "Excel") && x.EffectiveDate >= cutoff.AddDays(1)).SumAsync(x => x.Days);
        var target = agreement.AgreedLeaveDays + manualAfter;
        var leaves = await db.Leaves.Include(x => x.LeaveType).Where(x => x.EmployeeId == employee.Id && x.Status == 1).ToListAsync();
        foreach (var leave in leaves.Where(IsAnnual))
        {
            decimal before = 0;
            if (leave.StartDate < cutoff.AddDays(1))
                before = leave.EndDate <= cutoff.AddDays(1) ? leave.RequestedDays :
                    await BeforeCutoffAsync(leave, cutoff);
            var after = LeaveAccountingRules.DaysAfterCutoff(leave.RequestedDays, before);
            target -= after;
            var charge = await db.LeaveCharges.SingleOrDefaultAsync(x => x.LeaveId == leave.Id);
            if (charge == null) { charge = new LeaveCharge { EmployeeId = employee.Id, LeaveId = leave.Id }; db.LeaveCharges.Add(charge); }
            charge.Days = leave.RequestedDays; charge.IncludedInOpening = true; charge.HistoricalIncluded = true;
        }
        await MoveAsync(employee, account, target - account.Balance, "Reconciliation", "agreement:" + Guid.NewGuid(), actor,
            "Tarihli mutabakat uygulandı; sonraki manuel hareketler korundu.", cutoff);
        await AccrueAsync(employee, account, employee.TerminationDate?.Date < Today ? employee.TerminationDate.Value.Date : Today, actor);
    }

    public async Task<DateCorrectionPreview> PreviewDatesAsync(int id, DateTime hire, DateTime? birth, string actor)
    {
        await RequireAdminAsync(actor);
        return await WithEmployeeAsync(id, async employee =>
        {
            var account = await EnsureAccountAsync(employee);
            ValidatePersonalDates(hire, birth);
            var credits = await db.LeaveAccruals.Where(x => x.EmployeeId == id && x.Cycle == account.Cycle).ToListAsync();
            var needsReview = account.NeedsReview || (hire.Date != account.HireDate && account.CoveredServiceYears > 0) ||
                (birth?.Date != account.BirthDate && account.CoveredServiceYears > 0);
            var through = employee.TerminationDate?.Date < Today ? employee.TerminationDate.Value.Date : Today;
            var delta = ExpectedTotal(hire, birth, account.CoveredServiceYears, through) - credits.Sum(x => x.CreditedDays);
            return new DateCorrectionPreview(id, employee.HireDate, hire.Date, birth?.Date, account.Balance,
                needsReview ? 0 : delta, needsReview, Fingerprint(account, employee, hire, birth));
        });
    }

    public async Task CorrectDatesAsync(int id, DateTime hire, DateTime? birth, string token, string reason, string actor)
    {
        await RequireAdminAsync(actor);
        await WithEmployeeAsync(id, async employee =>
        {
            var account = await EnsureAccountAsync(employee);
            ValidatePersonalDates(hire, birth);
            if (token != Fingerprint(account, employee, hire, birth)) throw new InvalidOperationException("Kayıt değişti. Yeniden önizleme yapın.");
            if (account.NeedsReview || (account.CoveredServiceYears > 0 && (hire.Date != account.HireDate || birth?.Date != account.BirthDate)))
                throw new InvalidOperationException("Eski haklar belirsiz. Tarih düzeltmesini yeni mutabakatla birlikte yapın.");
            if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Gerekçe gerekiyor.");
            reason = $"Giriş: {employee.HireDate:yyyy-MM-dd} → {hire:yyyy-MM-dd}; doğum: {employee.BirthDate:yyyy-MM-dd} → {birth:yyyy-MM-dd}. " + reason;
            employee.HireDate = hire.Date; employee.BirthDate = birth?.Date;
            account.HireDate = hire.Date; account.BirthDate = birth?.Date;
            var through = employee.TerminationDate?.Date < Today ? employee.TerminationDate.Value.Date : Today;
            var credits = await db.LeaveAccruals.Where(x => x.EmployeeId == id && x.Cycle == account.Cycle).ToListAsync();
            foreach (var credit in credits)
            {
                var date = hire.Date.AddYears(credit.ServiceYear);
                var target = date <= through ? AnnualLeaveEntitlementCalculator.CalculateEntitlement(hire, birth, date) : 0;
                await MoveAsync(employee, account, target - credit.CreditedDays, "DateCorrection",
                    "date:" + Guid.NewGuid(), actor, reason, Today, serviceYear: credit.ServiceYear);
                credit.CreditedDays = target; credit.Anniversary = date;
            }
            // Zero movement records a correction even when no existing accrual was affected.
            await MoveAsync(employee, account, 0, "DateCorrection", "date:" + Guid.NewGuid(), actor, reason, Today);
            await AccrueAsync(employee, account, through, actor);
            return true;
        });
    }

    public static decimal ExpectedTotal(DateTime hire, DateTime? birth, int covered, DateTime through)
        => AnnualLeaveEntitlementCalculator.GetEntitlements(hire, birth, hire.AddYears(covered), through).Sum(x => x.Days);
    public static string ImportFingerprint(IEnumerable<(string Email, decimal? Days, string? Description)> rows)
        => Hash(JsonSerializer.Serialize(rows.Select(x => JsonSerializer.Serialize(new {
            Email=x.Email.Trim().ToLowerInvariant(), Days=x.Days?.ToString("G29", CultureInfo.InvariantCulture), Description=x.Description?.Trim() ?? ""
        })).OrderBy(x=>x,StringComparer.Ordinal)));
    public static string Hash(string input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
    private static string Fingerprint(LeaveAccount account, EmployeePortal employee, DateTime hire, DateTime? birth)
        => Hash(JsonSerializer.Serialize(new { account.Revision, account.Cycle, employee.HireDate, employee.BirthDate,
            employee.TerminationDate, employee.IsDeleted, Today, NewHire = hire.Date, NewBirth = birth?.Date }));
    public static void ValidatePersonalDates(DateTime hire, DateTime? birth)
    {
        if (hire.Year <= 1900 || hire.Date > Today || (birth.HasValue && (birth.Value.Year <= 1900 || birth.Value.Date > hire.Date)))
            throw new InvalidOperationException("İşe giriş ve doğum tarihlerini kontrol edin.");
    }
    public static bool IsAnnual(Leave leave) => string.Equals(leave.LeaveType?.Code, LeaveTypeCodes.Annual, StringComparison.OrdinalIgnoreCase);
}

public record DateCorrectionPreview(int EmployeeId, DateTime? OldHireDate, DateTime HireDate, DateTime? BirthDate,
    decimal Balance, decimal Difference, bool RequiresReconciliation, string Token);
public record CalendarDay(string Name, DateTime Start, DateTime End);
