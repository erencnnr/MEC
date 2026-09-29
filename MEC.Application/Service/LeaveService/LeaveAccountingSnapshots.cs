using System.Text.Json;
using MEC.Application.Abstractions.Service.LeaveService.Model;
using MEC.Domain.Common;
using MEC.Domain.Entity.Leave;
using Microsoft.EntityFrameworkCore;

namespace MEC.Application.Service.LeaveService;

public sealed partial class LeaveAccountingService
{
    public async Task<List<HolidayCalendarItemModel>> GetPublishedHolidaysAsync()
    {
        var calendars = await db.LeaveCalendars.AsNoTracking().Where(x => x.Year > 0 && x.IsApproved).ToListAsync();
        return calendars.SelectMany(x => JsonSerializer.Deserialize<List<CalendarDay>>(x.PublishedJson) ?? [])
            .OrderBy(x => x.Start).Select((x,i) => new HolidayCalendarItemModel { Id = -i-1, Name = x.Name, StartDate = x.Start, EndDate = x.End }).ToList();
    }
    public async Task CaptureCalculationAsync(Leave leave)
    {
        // Call only on creation or before a future leave starts; historic rules are immutable.
        var charge = await db.LeaveCharges.SingleOrDefaultAsync(x => x.LeaveId == leave.Id);
        if (charge == null) { charge = new LeaveCharge { LeaveId=leave.Id, EmployeeId=leave.EmployeeId }; db.LeaveCharges.Add(charge); }
        var calendars = await db.LeaveCalendars.Where(x => x.Year >= leave.StartDate.Year && x.Year <= leave.EndDate.Year && x.IsApproved).ToListAsync();
        var holidays = calendars.SelectMany(x => JsonSerializer.Deserialize<List<CalendarDay>>(x.PublishedJson) ?? []).ToList();
        var policies = await db.LeavePolicySettings.OrderBy(x=>x.EffectiveFrom).Select(x=>new SaturdayLeavePolicy(x.EffectiveFrom,x.CountSaturday)).ToListAsync();
        charge.CalculationJson = JsonSerializer.Serialize(new LeaveCalculationSnapshot(holidays, policies));
        await db.SaveChangesAsync();
    }
    private async Task<decimal> BeforeCutoffAsync(Leave leave, DateTime cutoff)
    {
        if (leave.EndDate <= cutoff.AddDays(1)) return leave.RequestedDays;
        if (leave.StartDate >= cutoff.AddDays(1)) return 0;
        var charge = await db.LeaveCharges.SingleOrDefaultAsync(x=>x.LeaveId==leave.Id);
        if (charge?.SplitCutoff == cutoff && charge.SplitBeforeDays.HasValue) return charge.SplitBeforeDays.Value;
        if (string.IsNullOrEmpty(charge?.CalculationJson))
            throw new InvalidOperationException($"İzin #{leave.Id} mutabakat tarihini aşıyor ve eski hesaplama kuralları belirsiz. İzin detayında admin, mutabakata dahil gün sayısını doğrulamalıdır.");
        var snapshot = JsonSerializer.Deserialize<LeaveCalculationSnapshot>(charge.CalculationJson)!;
        return Math.Min(leave.RequestedDays, LeaveDurationCalculator.CalculateRequestedDays(leave.StartDate, cutoff.AddDays(1),
            snapshot.Holidays.Select(x=>new HolidayInterval(x.Start,x.End)), snapshot.Policies));
    }
    public async Task ReviewSplitAsync(int leaveId, DateTime cutoff, decimal before, string reason, string actor)
    {
        await RequireAdminAsync(actor);
        var id=await db.Leaves.Where(x=>x.Id==leaveId).Select(x=>x.EmployeeId).SingleAsync();
        await WithEmployeeAsync(id,async employee=>{
            var leave=await db.Leaves.SingleAsync(x=>x.Id==leaveId);
            var account=await EnsureAccountAsync(employee);
            if (cutoff.Date>Today || before<0 || before>leave.RequestedDays || string.IsNullOrWhiteSpace(reason))
                throw new InvalidOperationException("Mutabakat tarihi, gün sayısı ve gerekçeyi kontrol edin.");
            var charge=await db.LeaveCharges.SingleOrDefaultAsync(x=>x.LeaveId==leaveId);
            if(charge==null){charge=new LeaveCharge{LeaveId=leaveId,EmployeeId=id};db.LeaveCharges.Add(charge);}
            charge.SplitCutoff=cutoff.Date;charge.SplitBeforeDays=before;charge.ReviewReason=actor+": "+reason;
            await MoveAsync(employee,account,0,"SplitReview","split:"+Guid.NewGuid(),actor,
                $"İzin #{leaveId}: {cutoff:yyyy-MM-dd} dahil {before} gün. "+reason,Today,leaveId);
            return true;
        });
    }
}
public record LeaveCalculationSnapshot(List<CalendarDay> Holidays, List<SaturdayLeavePolicy> Policies);
