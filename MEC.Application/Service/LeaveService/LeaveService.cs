using MEC.Application.Abstractions.Service.LeaveService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Employee;
using MEC.Domain.Entity.Leave;
using System.Linq;

public class LeaveService : ILeaveService
{
    private const string AnnualLeaveType = "Yıllık İzin";
    private readonly IGenericRepository<Leave> _leaveRepository;
    private readonly IGenericRepository<Employee> _employeeRepository;
    private readonly IGenericRepository<EmployeePortal> _employeePortalRepository;
    private const int ApprovedStatus = 1;

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

        var requestedDays = leave.RequestedDays > 0
            ? leave.RequestedDays
            : (int)(leave.EndDate.Date - leave.StartDate.Date).TotalDays + 1;
        var affectsAnnualBalance = string.Equals(leave.LeaveType, AnnualLeaveType, StringComparison.Ordinal);

        if (leave.Status != status)
        {
            var employee = await _employeeRepository.GetByIdAsync(leave.EmployeeId);
            if (employee != null && !string.IsNullOrWhiteSpace(employee.Email))
            {
                var employeePortal = (await _employeePortalRepository.GetAllAsync(x => x.Email == employee.Email)).FirstOrDefault();
                if (employeePortal != null)
                {
                    if (affectsAnnualBalance && leave.Status != ApprovedStatus && status == ApprovedStatus)
                    {
                        employeePortal.LeaveDays -= requestedDays;
                    }
                    else if (affectsAnnualBalance && leave.Status == ApprovedStatus && status != ApprovedStatus)
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
