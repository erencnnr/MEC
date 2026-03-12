using MEC.Application.Abstractions.Service.LeaveService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Leave;

public class LeaveService : ILeaveService
{
    private readonly IGenericRepository<Leave> _leaveRepository;

    public LeaveService(IGenericRepository<Leave> leaveRepository)
    {
        _leaveRepository = leaveRepository;
    }

    public async Task<List<Leave>> GetAllLeavesAsync()
    {
        var leaves = await _leaveRepository.GetAllAsync();

        return leaves
            .OrderByDescending(x => x.CreatedDate)
            .ToList();
    }

    public async Task<bool> UpdateLeaveStatusAsync(int leaveId, int status)
    {
        var leave = await _leaveRepository.GetByIdAsync(leaveId);

        if (leave == null)
            return false;

        leave.Status = status;

        _leaveRepository.Update(leave);

        return true;
    }
}