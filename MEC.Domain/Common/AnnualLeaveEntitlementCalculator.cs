namespace MEC.Domain.Common
{
    public static class AnnualLeaveEntitlementCalculator
    {
        public static decimal CalculateEntitlement(
            DateTime hireDate,
            DateTime? birthDate,
            DateTime entitlementDate)
        {
            var completedServiceYears = GetCompletedYears(hireDate.Date, entitlementDate.Date);
            if (completedServiceYears < 1)
            {
                return 0m;
            }

            decimal entitlement = completedServiceYears switch
            {
                <= 5 => 14m,
                < 15 => 20m,
                _ => 26m
            };

            if (birthDate.HasValue)
            {
                var age = GetCompletedYears(birthDate.Value.Date, entitlementDate.Date);
                if (age <= 18 || age >= 50)
                {
                    entitlement = Math.Max(entitlement, 20m);
                }
            }

            return entitlement;
        }

        public static IEnumerable<(DateTime EntitlementDate, decimal Days)> GetEntitlements(
            DateTime hireDate,
            DateTime? birthDate,
            DateTime afterDate,
            DateTime throughDate)
        {
            if (throughDate.Date <= afterDate.Date)
            {
                yield break;
            }

            var completedYearsAtEnd = GetCompletedYears(hireDate.Date, throughDate.Date);
            for (var serviceYear = 1; serviceYear <= completedYearsAtEnd; serviceYear++)
            {
                var entitlementDate = hireDate.Date.AddYears(serviceYear);
                if (entitlementDate <= afterDate.Date || entitlementDate > throughDate.Date)
                {
                    continue;
                }

                yield return (
                    entitlementDate,
                    CalculateEntitlement(hireDate, birthDate, entitlementDate));
            }
        }

        private static int GetCompletedYears(DateTime startDate, DateTime endDate)
        {
            var years = endDate.Year - startDate.Year;
            if (startDate.AddYears(years) > endDate)
            {
                years--;
            }

            return years;
        }
    }
}
