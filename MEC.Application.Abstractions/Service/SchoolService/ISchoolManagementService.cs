using MEC.Application.Abstractions.Common.Models;

namespace MEC.Application.Abstractions.Service.SchoolService;

public interface ISchoolManagementService
{
    Task<SchoolManagementModel> GetAsync();
    Task<OperationResultModel> AssignManagerAsync(int locationId, int? employeeId);
}

public sealed class SchoolManagementModel
{
    public List<SchoolManagerItem> Schools { get; set; } = new();
    public List<SchoolManagerOption> Users { get; set; } = new();
}

public sealed record SchoolManagerItem(int Id, string Name, int? ManagerId);
public sealed record SchoolManagerOption(int Id, string Name);
