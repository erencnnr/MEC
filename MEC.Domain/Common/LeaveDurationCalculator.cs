namespace MEC.Domain.Common
{
    public static class LeaveDurationCalculator
    {
        public static readonly TimeOnly WorkDayStart = new(9, 0);
        public static readonly TimeOnly WorkDayEnd = new(18, 0);
        private const decimal WorkHoursPerDay = 9m;

        public static bool IsWithinWorkingHours(DateTime value)
        {
            var time = TimeOnly.FromDateTime(value);
            return time >= WorkDayStart && time <= WorkDayEnd;
        }

        public static decimal CalculateRequestedDays(
            DateTime startDateTime,
            DateTime endDateTime,
            IEnumerable<HolidayInterval>? holidays = null,
            IEnumerable<SaturdayLeavePolicy>? saturdayPolicies = null)
        {
            if (endDateTime < startDateTime)
            {
                return 0m;
            }

            var holidayIntervals = holidays?
                .Where(x => x.EndDate > x.StartDate)
                .ToList() ?? new List<HolidayInterval>();
            var orderedSaturdayPolicies = saturdayPolicies?
                .OrderBy(x => x.EffectiveFrom)
                .ToList() ?? new List<SaturdayLeavePolicy>();
            decimal totalMinutes = 0m;

            for (var day = startDateTime.Date; day <= endDateTime.Date; day = day.AddDays(1))
            {
                if (IsNonWorkingDay(day, orderedSaturdayPolicies))
                {
                    continue;
                }

                var dayWorkStart = day.Add(WorkDayStart.ToTimeSpan());
                var dayWorkEnd = day.Add(WorkDayEnd.ToTimeSpan());

                var effectiveStart = startDateTime > dayWorkStart ? startDateTime : dayWorkStart;
                var effectiveEnd = endDateTime < dayWorkEnd ? endDateTime : dayWorkEnd;

                if (effectiveEnd <= effectiveStart)
                {
                    continue;
                }

                var dayMinutes = (decimal)(effectiveEnd - effectiveStart).TotalMinutes;
                var holidayMinutes = CalculateHolidayOverlapMinutes(effectiveStart, effectiveEnd, holidayIntervals);
                totalMinutes += Math.Max(0m, dayMinutes - holidayMinutes);
            }

            var rawDays = (totalMinutes / 60m) / WorkHoursPerDay;
            return RoundRequestedDays(rawDays);
        }

        public static decimal RoundRequestedDays(decimal rawDays)
        {
            if (rawDays <= 0m)
            {
                return 0m;
            }

            return Math.Ceiling(rawDays * 2m) / 2m;
        }

        private static bool IsNonWorkingDay(
            DateTime value,
            IReadOnlyList<SaturdayLeavePolicy> saturdayPolicies)
        {
            if (value.DayOfWeek == DayOfWeek.Sunday)
            {
                return true;
            }

            if (value.DayOfWeek != DayOfWeek.Saturday)
            {
                return false;
            }

            var effectivePolicy = saturdayPolicies
                .LastOrDefault(x => x.EffectiveFrom.Date <= value.Date);

            return !effectivePolicy.CountsAsLeaveDay;
        }

        private static decimal CalculateHolidayOverlapMinutes(
            DateTime effectiveStart,
            DateTime effectiveEnd,
            IReadOnlyCollection<HolidayInterval> holidays)
        {
            if (holidays.Count == 0)
            {
                return 0m;
            }

            var overlaps = holidays
                .Where(x => x.StartDate < effectiveEnd && x.EndDate > effectiveStart)
                .Select(x => new
                {
                    Start = x.StartDate > effectiveStart ? x.StartDate : effectiveStart,
                    End = x.EndDate < effectiveEnd ? x.EndDate : effectiveEnd
                })
                .Where(x => x.End > x.Start)
                .OrderBy(x => x.Start)
                .ToList();

            if (overlaps.Count == 0)
            {
                return 0m;
            }

            decimal totalMinutes = 0m;
            var currentStart = overlaps[0].Start;
            var currentEnd = overlaps[0].End;

            foreach (var overlap in overlaps.Skip(1))
            {
                if (overlap.Start <= currentEnd)
                {
                    if (overlap.End > currentEnd)
                    {
                        currentEnd = overlap.End;
                    }

                    continue;
                }

                totalMinutes += (decimal)(currentEnd - currentStart).TotalMinutes;
                currentStart = overlap.Start;
                currentEnd = overlap.End;
            }

            totalMinutes += (decimal)(currentEnd - currentStart).TotalMinutes;
            return totalMinutes;
        }
    }
}
