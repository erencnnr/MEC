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

        public static decimal CalculateRequestedDays(DateTime startDateTime, DateTime endDateTime)
        {
            if (endDateTime < startDateTime)
            {
                return 0m;
            }

            decimal totalHours = 0m;

            for (var day = startDateTime.Date; day <= endDateTime.Date; day = day.AddDays(1))
            {
                if (IsWeekend(day))
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

                totalHours += (decimal)(effectiveEnd - effectiveStart).TotalHours;
            }

            var rawDays = totalHours / WorkHoursPerDay;
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

        private static bool IsWeekend(DateTime value)
        {
            return value.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        }
    }
}
