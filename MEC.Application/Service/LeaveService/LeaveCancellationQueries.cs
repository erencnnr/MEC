using MEC.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace MEC.Application.Service.LeaveService;

public sealed partial class LeaveAccountingService
{
    private async Task RequireCancellationApproverAsync(string actor)
    {
        var who = await workflow.GetActorAsync(actor);
        if (who == null || (!who.IsAdministrator && !who.IsFinalApprover)) throw new UnauthorizedAccessException();
    }

    private IQueryable<CancellationListItem> CancellationQuery()
    {
        var now = LeaveAccountingRules.Now;
        return from cancellation in db.LeaveCancellations.AsNoTracking()
               join leave in db.Leaves.AsNoTracking() on cancellation.LeaveId equals leave.Id
               join employee in db.EmployeePortals.AsNoTracking() on leave.EmployeeId equals employee.Id
               select new CancellationListItem
               {
                   Id = cancellation.Id, LeaveId = leave.Id, EmployeeId = employee.Id,
                   EmployeeName = employee.FirstName + " " + employee.LastName, Email = employee.Email,
                   LeaveType = leave.LeaveType == null ? "" : leave.LeaveType.Name, StartDate = leave.StartDate, EndDate = leave.EndDate,
                   Days = leave.RequestedDays, RequestedAt = cancellation.CreatedDate,
                   Reason = cancellation.Reason, RequestedBy = cancellation.RequestedBy,
                   Status = cancellation.Status == "Pending" && (leave.StartDate <= now || leave.Status != 1)
                       ? "Expired" : cancellation.Status,
                   DecidedBy = cancellation.DecidedBy, DecisionDate = cancellation.DecisionDate
               };
    }

    public async Task<CancellationListPage> GetCancellationsAsync(string actor, CancellationFilter? filter = null)
    {
        await RequireCancellationApproverAsync(actor);
        filter ??= new();
        filter.Search = (filter.Search ?? "").Trim();
        if (filter.Search.Length > 100) throw new InvalidOperationException("Arama metni en fazla 100 karakter olabilir.");
        if (filter.Status is not ("All" or "Pending" or "Approved" or "Rejected" or "Expired"))
            throw new InvalidOperationException("Geçerli bir talep durumu seçin.");
        if (filter.From.HasValue && filter.To.HasValue && filter.From.Value.Date > filter.To.Value.Date)
            throw new InvalidOperationException("Başlangıç filtresi bitiş filtresinden sonra olamaz.");

        var query = CancellationQuery();
        if (filter.Search.Length > 0)
            query = query.Where(x => x.EmployeeName.Contains(filter.Search) || x.Email.Contains(filter.Search));
        if (filter.Status != "All") query = query.Where(x => x.Status == filter.Status);
        if (filter.From.HasValue) query = query.Where(x => x.StartDate.Date >= filter.From.Value.Date);
        if (filter.To.HasValue) query = query.Where(x => x.StartDate.Date <= filter.To.Value.Date);
        var count = await query.CountAsync();
        filter.Page = Math.Clamp(filter.Page, 1, Math.Max(1, (int)Math.Ceiling(count / 25d)));
        return new CancellationListPage
        {
            Filter = filter, TotalCount = count,
            Items = await query.OrderBy(x => x.Status == "Pending" ? 0 : 1).ThenByDescending(x => x.Id)
                .Skip((filter.Page - 1) * 25).Take(25).ToListAsync()
        };
    }

    public async Task<CancellationListItem?> GetCancellationAsync(int leaveId, string actor)
    {
        await RequireCancellationApproverAsync(actor);
        return await CancellationQuery().SingleOrDefaultAsync(x => x.LeaveId == leaveId);
    }
}

public sealed class CancellationFilter
{
    public string? Search { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class CancellationListPage
{
    public CancellationFilter Filter { get; set; } = new();
    public List<CancellationListItem> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / 25d));
}

public sealed class CancellationListItem
{
    public int Id { get; set; }
    public int LeaveId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public string Email { get; set; } = "";
    public string LeaveType { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Days { get; set; }
    public DateTime? RequestedAt { get; set; }
    public string RequestedBy { get; set; } = "";
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public string DecidedBy { get; set; } = "";
    public DateTime? DecisionDate { get; set; }
}
