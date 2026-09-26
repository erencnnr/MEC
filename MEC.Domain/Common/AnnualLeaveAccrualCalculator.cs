using MEC.Domain.Entity.Employee;

namespace MEC.Domain.Common;

public static class AnnualLeaveAccrualCalculator
{
    public static decimal PendingDays(EmployeePortal employee, DateTime today)
    {
        if (employee.IsDeleted || !employee.HireDate.HasValue || employee.HireDate.Value.Year <= 1900)
            return 0m;
        var after = employee.AnnualLeaveProcessedThrough ?? today.Date.AddDays(-1);
        var through = employee.TerminationDate.HasValue && employee.TerminationDate.Value.Date < today.Date
            ? employee.TerminationDate.Value.Date : today.Date;
        return AnnualLeaveEntitlementCalculator.GetEntitlements(
            employee.HireDate.Value, employee.BirthDate, after, through).Sum(x => x.Days);
    }
}
