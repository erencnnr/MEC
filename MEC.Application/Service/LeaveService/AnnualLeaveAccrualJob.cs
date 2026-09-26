using MEC.Application.Abstractions.Service.LeaveService;
using MEC.DAL.Config.Contexts;
using MEC.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MEC.Application.Service.LeaveService;

public sealed class AnnualLeaveAccrualJob(
    ApplicationDbContext db,
    ILeaveService leaveService,
    ILogger<AnnualLeaveAccrualJob> logger)
{
    public async Task<int> RunAsync(DateTime today, CancellationToken cancellationToken = default)
    {
        today = today.Date;
        var ids = await db.EmployeePortals.AsNoTracking()
            .Where(x => !x.IsDeleted && x.HireDate.HasValue && x.HireDate.Value.Year > 1900 &&
                        (!x.AnnualLeaveProcessedThrough.HasValue || x.AnnualLeaveProcessedThrough < today))
            .Select(x => x.Id).ToListAsync(cancellationToken);
        var updated = 0;
        foreach (var id in ids)
        {
            // Lock and checkpoint are committed together, including when two job processes overlap.
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var employee = (await db.EmployeePortals
                .FromSqlInterpolated($"SELECT * FROM employee_portal WHERE Id = {id} FOR UPDATE")
                .ToListAsync(cancellationToken)).Single();
            if (employee.IsDeleted || !employee.HireDate.HasValue || employee.HireDate.Value.Year <= 1900 ||
                employee.AnnualLeaveProcessedThrough >= today)
            {
                await transaction.CommitAsync(cancellationToken);
                db.ChangeTracker.Clear();
                continue;
            }

            var previousBalance = employee.LeaveDays;
            var reconciled = await leaveService.RecalculateEmployeeAnnualBalanceAsync(employee, today);
            if (!reconciled)
            {
                // On first deployment the stored balance already includes historical rights.
                // Start from yesterday; subsequent runs also catch up missed anniversaries.
                employee.LeaveDays += AnnualLeaveAccrualCalculator.PendingDays(employee, today);
            }

            employee.AnnualLeaveProcessedThrough = today;
            employee.UpdateDate = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            if (employee.LeaveDays != previousBalance)
            {
                updated++;
                logger.LogInformation("Annual leave: employee {EmployeeId}, date {Date}, balance {Before} -> {After}",
                    id, today, previousBalance, employee.LeaveDays);
            }
            db.ChangeTracker.Clear();
        }
        logger.LogInformation("Annual leave job finished: {Checked} checked, {Updated} balances changed, date {Date}",
            ids.Count, updated, today);
        return updated;
    }
}
