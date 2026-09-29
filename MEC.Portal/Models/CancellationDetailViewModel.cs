using System.Globalization;
using MEC.Application.Service.LeaveService;

namespace MEC.Portal.Models;

public sealed class CancellationDetailViewModel
{
    public CancellationListItem Item { get; set; } = new();
    public string ReturnUrl { get; set; } = "/Admin/LeaveCancellations";
}

public static class CancellationDisplay
{
    public static string Status(string value) => value switch
    {
        "Pending" => "Bekliyor", "Approved" => "Kabul edildi",
        "Rejected" => "Reddedildi", "Expired" => "Süresi geçti", _ => "Bilinmiyor"
    };
    public static string Time(DateTime? utc) => utc.HasValue
        ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc),
            TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "Turkey Standard Time" : "Europe/Istanbul"))
            .ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")) : "Belirtilmemiş";
}
