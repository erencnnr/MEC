using MEC.Application.Abstractions.Service.LeaveService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Common;
using MEC.Domain.Common.Enum;
using MEC.Domain.Entity.Employee;
using MEC.Domain.Entity.Leave;
using System.Linq;

public class LeaveService : ILeaveService
{
    private readonly IGenericRepository<Leave> _leaveRepository;
    private readonly IGenericRepository<Employee> _employeeRepository;
    private readonly IGenericRepository<EmployeePortal> _employeePortalRepository;

    public LeaveService(
        IGenericRepository<Leave> leaveRepository,
        IGenericRepository<Employee> employeeRepository,
        IGenericRepository<EmployeePortal> employeePortalRepository)
    {
        _leaveRepository = leaveRepository;
        _employeeRepository = employeeRepository;
        _employeePortalRepository = employeePortalRepository;
    }

    public async Task<List<Leave>> GetAllLeavesAsync()
    {
        var leaves = await _leaveRepository.GetAllAsync(null, x => x.LeaveType);

        return leaves
            .OrderByDescending(x => x.CreatedDate)
            .ToList();
    }

    public async Task<bool> UpdateLeaveStatusAsync(int leaveId, int status)
    {
        var leave = (await _leaveRepository.GetAllAsync(x => x.Id == leaveId, x => x.LeaveType)).FirstOrDefault();

        if (leave == null)
        {
            return false;
        }

        var requestedDays = leave.RequestedDays > 0
            ? leave.RequestedDays
            : LeaveDurationCalculator.CalculateRequestedDays(leave.StartDate, leave.EndDate);
        var affectsAnnualBalance = string.Equals(leave.LeaveType?.Code, LeaveTypeCodes.Annual, StringComparison.OrdinalIgnoreCase);

        if (leave.Status != status)
        {
            var employee = await _employeeRepository.GetByIdAsync(leave.EmployeeId);
            if (employee != null && !string.IsNullOrWhiteSpace(employee.Email))
            {
                var employeePortal = (await _employeePortalRepository.GetAllAsync(x => x.Email == employee.Email)).FirstOrDefault();
                if (employeePortal != null)
                {
                    if (affectsAnnualBalance && leave.Status != (int)LeaveStatus.Approved && status == (int)LeaveStatus.Approved)
                    {
                        employeePortal.LeaveDays -= requestedDays;
                    }
                    else if (affectsAnnualBalance && leave.Status == (int)LeaveStatus.Approved && status != (int)LeaveStatus.Approved)
                    {
                        employeePortal.LeaveDays += requestedDays;
                    }

                    leave.RemainingLeaveDays = employeePortal.LeaveDays;
                    _employeePortalRepository.Update(employeePortal);
                }
            }
        }

        leave.Status = status;
        leave.RequestedDays = requestedDays;

        _leaveRepository.Update(leave);

        return true;
    }
}
