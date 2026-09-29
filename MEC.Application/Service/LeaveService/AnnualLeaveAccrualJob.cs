using MEC.DAL.Config.Contexts;
using MEC.Domain.Common;
using MEC.Domain.Entity.Leave;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace MEC.Application.Service.LeaveService;
public sealed class AnnualLeaveAccrualJob(ApplicationDbContext db, LeaveAccountingService accounting, ILogger<AnnualLeaveAccrualJob> logger)
{
    public async Task<int> RunAsync(DateTime today, CancellationToken cancellationToken = default)
    {
        var runId = Guid.NewGuid().ToString("N");
        await accounting.ExpireCancellationsAsync();
        var ids = await db.EmployeePortals.AsNoTracking().Where(x => !x.IsDeleted).OrderBy(x => x.Id).Select(x => x.Id).ToListAsync(cancellationToken);
        var updated = 0; var failures = 0;
        foreach (var id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = new LeaveJobResult { RunId = runId, EmployeeId = id, BusinessDate = today.Date, CreatedDate = DateTime.UtcNow };
            try
            {
                await accounting.WithEmployeeAsync(id, async employee =>
                {
                    if (employee.IsDeleted) return false;
                    var before = employee.LeaveDays;
                    var account = await accounting.EnsureAccountAsync(employee);
                    await accounting.AccrueAsync(employee, account, today, "annual-job");
                    if (before != employee.LeaveDays) updated++;
                    return true;
                });
                result.Success = true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failures++; db.ChangeTracker.Clear();
                result.Error = ex.GetBaseException().Message[..Math.Min(2000, ex.GetBaseException().Message.Length)];
                logger.LogError(ex, "Annual leave failed for employee {EmployeeId}; continuing", id);
            }
            db.LeaveJobResults.Add(result);
            await db.SaveChangesAsync(cancellationToken); db.ChangeTracker.Clear();
        }
        logger.LogInformation("Annual leave {Run}: {Checked} checked, {Updated} updated, {Failed} failed", runId, ids.Count, updated, failures);
        if (failures > 0) throw new InvalidOperationException($"{failures} personelin izin hesabı tamamlanamadı. Job sonuçlarını kontrol edin.");
        return updated;
    }
}
