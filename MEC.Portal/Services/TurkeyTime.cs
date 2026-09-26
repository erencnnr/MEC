namespace MEC.Portal.Services;

public static class TurkeyTime
{
    public static DateTime GetDate(DateTime utcNow)
    {
        var zoneId = OperatingSystem.IsWindows() ? "Turkey Standard Time" : "Europe/Istanbul";
        var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        return TimeZoneInfo.ConvertTimeFromUtc(utcNow, zone).Date;
    }
}
