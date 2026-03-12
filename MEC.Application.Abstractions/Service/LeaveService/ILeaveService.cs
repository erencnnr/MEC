using MEC.Domain.Entity.Employee;
using MEC.Domain.Entity.Leave;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MEC.Application.Abstractions.Service.LeaveService
{
    public interface ILeaveService
    {
        // Tüm izin taleplerini getirecek metodumuz
        Task<List<Leave>> GetAllLeavesAsync();

        // İleride onaylama/reddetme için kullanacağımız metod
        Task<bool> UpdateLeaveStatusAsync(int leaveId, int status);
    }
}