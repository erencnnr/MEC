using MEC.Application.Abstractions.Common.Models;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.DAL.Config.Contexts;
using Microsoft.EntityFrameworkCore;

namespace MEC.Application.Service.SchoolService;

public sealed class SchoolManagementService(ApplicationDbContext db) : ISchoolManagementService
{
    public async Task<SchoolManagementModel> GetAsync() => new()
    {
        Schools = await db.Locations.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new SchoolManagerItem(x.Id, x.Name, x.ManagerEmployeePortalId)).ToListAsync(),
        Users = await db.EmployeePortals.AsNoTracking()
            .Where(x => !x.IsDeleted && (!x.TerminationDate.HasValue || x.TerminationDate > DateTime.Today))
            .OrderBy(x => x.FirstName).ThenBy(x => x.LastName)
            .Select(x => new SchoolManagerOption(x.Id, x.FirstName + " " + x.LastName + " (" + x.Email + ")"))
            .ToListAsync()
    };

    public async Task<OperationResultModel> AssignManagerAsync(int locationId, int? employeeId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        // Serialize assignment changes so the compatibility IsManager flags stay consistent.
        var schools = await db.Locations.FromSqlRaw("SELECT * FROM location ORDER BY Id FOR UPDATE").ToListAsync();
        var school = schools.FirstOrDefault(x => x.Id == locationId);
        if (school == null) return new() { IsSuccess = false, Message = "Okul bulunamadı." };
        if (employeeId.HasValue && !await db.EmployeePortals.AnyAsync(x => x.Id == employeeId &&
                !x.IsDeleted && (!x.TerminationDate.HasValue || x.TerminationDate > DateTime.Today)))
            return new() { IsSuccess = false, Message = "Aktif bir kullanıcı seçiniz." };

        school.ManagerEmployeePortalId = employeeId;
        school.UpdateDate = DateTime.UtcNow;
        await db.SaveChangesAsync();
        var managerIds = schools.Where(x => x.ManagerEmployeePortalId.HasValue)
            .Select(x => x.ManagerEmployeePortalId!.Value).Distinct().ToList();
        await db.EmployeePortals.Where(x => x.IsManager && !managerIds.Contains(x.Id))
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsManager, false));
        await db.EmployeePortals.Where(x => managerIds.Contains(x.Id) && !x.IsManager)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsManager, true));
        await transaction.CommitAsync();
        return new() { IsSuccess = true, Message = "Okul müdürü kaydedildi." };
    }
}
