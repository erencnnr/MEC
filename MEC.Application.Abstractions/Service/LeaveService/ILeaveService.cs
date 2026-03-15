using MEC.Domain.Entity.Leave;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MEC.Application.Abstractions.Service.LeaveService
{
    public interface ILeaveService
    {
        Task<List<Leave>> GetAllLeavesAsync();
        Task<bool> UpdateLeaveStatusAsync(int leaveId, int status);
    }
}
