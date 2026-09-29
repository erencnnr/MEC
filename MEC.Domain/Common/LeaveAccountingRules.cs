namespace MEC.Domain.Common;

public static class LeaveAccountingRules
{
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
        TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "Turkey Standard Time" : "Europe/Istanbul"));
    public static int CompletedYears(DateTime? hire, DateTime through)
    {
        if (!hire.HasValue || hire.Value.Year <= 1900 || hire.Value.Date > through.Date) return 0;
        var years = through.Year - hire.Value.Year;
        return hire.Value.Date.AddYears(years) > through.Date ? years - 1 : years;
    }
    public static bool Overlaps(DateTime start, DateTime end, DateTime otherStart, DateTime otherEnd)
        => start < otherEnd && end > otherStart;
    public static decimal DaysAfterCutoff(decimal total, decimal beforeCutoff)
        => Math.Max(0, total - Math.Min(total, beforeCutoff));
}
