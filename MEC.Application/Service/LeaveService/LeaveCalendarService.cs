using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using MEC.DAL.Config.Contexts;
using MEC.Domain.Common;
using MEC.Domain.Entity.Leave;
using Microsoft.EntityFrameworkCore;

namespace MEC.Application.Service.LeaveService;

public sealed class LeaveCalendarService(HttpClient http, ApplicationDbContext db, LeaveAccountingService accounting)
{
    private const string Origin = "https://vakithesaplama.diyanet.gov.tr/";
    public async Task<List<LeaveCalendar>> ListAsync(string actor)
    {
        await accounting.RequireAdminAsync(actor);
        return await db.LeaveCalendars.AsNoTracking().Where(x => x.Year > 0).OrderByDescending(x => x.Year).ToListAsync();
    }
    public async Task ImportAsync(int year, string actor)
    {
        await accounting.RequireAdminAsync(actor); ValidateYear(year);
        var (days, url) = await ReadOfficialAsync(year);
        await SaveDraftAsync(year, JsonSerializer.Serialize(days), url, actor);
    }
    // Kept separate from persistence so the real source can be checked without touching personnel data.
    public async Task<(List<CalendarDay> Days, string SourceUrl)> ReadOfficialAsync(int year)
    {
        ValidateYear(year);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        var index = await ReadHtmlAsync(Origin, timeout.Token);
        var url = FindOfficialUrl(index, year);
        var days = ParseDiyanet(await ReadHtmlAsync(url, timeout.Token), year);
        ValidateComplete(days, year);
        return (days, url);
    }
    private async Task<string> ReadHtmlAsync(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("MEC-Portal/1.0");
        request.Headers.Accept.ParseAdd("text/html");
        using var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
    public static string FindOfficialUrl(string index, int year)
    {
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        // Diyanet uses both quoted and unquoted hrefs, and two different yearly page routes.
        foreach (Match link in Regex.Matches(index,
            "<a\\b[^>]*\\bhref\\s*=\\s*(?:\"(?<url>[^\"]*)\"|'(?<url>[^']*)'|(?<url>[^\\s>]+))[^>]*>(?<name>[\\s\\S]*?)</a>", RegexOptions.IgnoreCase))
        {
            var name = Plain(link.Groups["name"].Value).ToUpper(culture);
            if (!Regex.IsMatch(name, $@"\b{year}\b") || !name.Contains("DİNİ GÜNLER")) continue;
            if (!Uri.TryCreate(new Uri(Origin), WebUtility.HtmlDecode(link.Groups["url"].Value), out var url) ||
                url.Host != new Uri(Origin).Host || url.Scheme != "https" || !url.IsDefaultPort || url.UserInfo.Length > 0) continue;
            if (Regex.IsMatch(url.PathAndQuery, @"^/(?:icerik\.php\?icerik=\d+|dinigunler\.php\?yil=\d+)$"))
                return url.ToString();
        }
        throw new InvalidOperationException($"Diyanet sitesinde {year} yılına ait dinî günler listesi bulunamadı. Takvimi elle girebilirsiniz.");
    }
    public static List<CalendarDay> ParseDiyanet(string html, int year)
    {
        var days = FixedDays(year);
        var religious = new List<CalendarDay>();
        foreach (Match row in Regex.Matches(html, "<tr\\b[^>]*>([\\s\\S]*?)</tr>", RegexOptions.IgnoreCase))
        {
            var cells = Regex.Matches(row.Value, "<t[dh]\\b[^>]*>([\\s\\S]*?)</t[dh]>", RegexOptions.IgnoreCase)
                .Select(x => Plain(x.Groups[1].Value)).ToList();
            if (cells.Count < 7) continue;
            var name = cells[^1].ToUpper(new CultureInfo("tr-TR"));
            if (!name.Contains("BAYRAMI") && !name.Contains("AREFE") && !name.Contains("ARİFE")) continue;
            if (!int.TryParse(cells[3], out var day)) continue;
            var text = cells[4].Replace("-", " ").Trim();
            if (!DateTime.TryParse(day + " " + text, new CultureInfo("tr-TR"), DateTimeStyles.AllowWhiteSpaces, out var date) || date.Year != year) continue;
            var half = name.Contains("AREFE") || name.Contains("ARİFE");
            religious.Add(new CalendarDay(cells[^1], date.Date.AddHours(half ? 13 : 0), date.Date.AddDays(1)));
        }
        if (religious.Count(x => x.Name.Contains("RAMAZAN", StringComparison.OrdinalIgnoreCase)) != 3 ||
            religious.Count(x => x.Name.Contains("KURBAN", StringComparison.OrdinalIgnoreCase)) != 4 ||
            religious.Count(x => x.Start.Hour == 13) != 2 || religious.Select(x => x.Start.Date).Distinct().Count() != 9)
            throw new InvalidOperationException("Bayram ve arife tarihleri eksiksiz doğrulanamadı. Aktarım kullanılmadı.");
        days.AddRange(religious);
        return days.OrderBy(x => x.Start).ToList();
    }
    public static List<CalendarDay> FixedDays(int year)
    {
        var result = new List<CalendarDay>();
        foreach (var (month, day, name) in new[] { (1,1,"Yılbaşı"), (4,23,"23 Nisan"), (5,1,"1 Mayıs"),
            (5,19,"19 Mayıs"), (7,15,"15 Temmuz"), (8,30,"30 Ağustos"), (10,29,"29 Ekim") })
        { var date = new DateTime(year, month, day); result.Add(new(name, date, date.AddDays(1))); }
        result.Add(new("28 Ekim öğleden sonra", new DateTime(year,10,28,13,0,0), new DateTime(year,10,29)));
        return result;
    }
    public static void ValidateComplete(List<CalendarDay> days, int year)
    {
        if (FixedDays(year).Any(required => !days.Any(x=>x.Start==required.Start && x.End==required.End)))
            throw new InvalidOperationException("Sabit resmî tatiller eksik.");
        var religious=days.Where(x=>x.Name.Contains("RAMAZAN",StringComparison.OrdinalIgnoreCase) ||
            x.Name.Contains("KURBAN",StringComparison.OrdinalIgnoreCase) ||
            x.Name.Contains("ARİFE",StringComparison.OrdinalIgnoreCase) || x.Name.Contains("AREFE",StringComparison.OrdinalIgnoreCase)).ToList();
        if(religious.Count(x=>x.Start.TimeOfDay==TimeSpan.Zero && x.Name.Contains("RAMAZAN",StringComparison.OrdinalIgnoreCase))!=3 ||
            religious.Count(x=>x.Start.TimeOfDay==TimeSpan.Zero && x.Name.Contains("KURBAN",StringComparison.OrdinalIgnoreCase))!=4 ||
            religious.Count(x=>x.Start.Hour==13)!=2 || religious.Count!=9 ||
            religious.Any(x=>x.Start.Year!=year || x.End!=x.Start.Date.AddDays(1)) ||
            religious.Select(x=>x.Start.Date).Distinct().Count()!=9)
            throw new InvalidOperationException("Ramazan bayramının 3 günü, Kurban bayramının 4 günü ve iki arife (13:00 sonrası) ayrı satırlar olarak girilmelidir.");
    }
    private static string Plain(string html) => Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(html,"<[^>]+>"," ")), @"\s+", " ").Trim();
    private static void ValidateYear(int year)
    { if (year < 2000 || year > LeaveAccountingService.Today.Year + 10) throw new InvalidOperationException("Takvim yılı geçersiz."); }
    public async Task SaveDraftAsync(int year, string json, string source, string actor)
    {
        await accounting.RequireAdminAsync(actor); ValidateYear(year);
        var entries = JsonSerializer.Deserialize<List<CalendarDay>>(json) ?? [];
        if (entries.Any(x => string.IsNullOrWhiteSpace(x.Name) || x.Name.Length > 200 || x.Start.Year != year ||
            x.End <= x.Start || x.End > new DateTime(year + 1, 1, 1))) throw new InvalidOperationException("Tatil tarihleri geçersiz.");
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync();
        await accounting.LockPolicyAsync();
        var calendar = await db.LeaveCalendars.SingleOrDefaultAsync(x => x.Year == year);
        if (calendar == null) { calendar = new LeaveCalendar { Year = year, CreatedDate = DateTime.UtcNow }; db.LeaveCalendars.Add(calendar); }
        calendar.DraftJson = JsonSerializer.Serialize(entries.OrderBy(x => x.Start));
        calendar.SourceUrl = source; calendar.Revision++; calendar.UpdateDate = DateTime.UtcNow;
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task<PolicyPreview> PreviewAsync(int? year, DateTime? effective, bool saturday, string actor)
    {
        await accounting.RequireAdminAsync(actor);
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync();
        await accounting.LockPolicyAsync();
        var result = await BuildPreviewAsync(year, effective, saturday);
        await tx.RollbackAsync(); db.ChangeTracker.Clear();
        return result;
    }
    private async Task<PolicyPreview> BuildPreviewAsync(int? year, DateTime? effective, bool saturday)
    {
        if (year.HasValue)
        {
            var calendar = await db.LeaveCalendars.SingleAsync(x => x.Year == year);
            ValidateComplete(JsonSerializer.Deserialize<List<CalendarDay>>(calendar.DraftJson) ?? [], year.Value);
            calendar.PublishedJson = calendar.DraftJson; calendar.IsApproved = true;
        }
        else
        {
            if (!effective.HasValue || effective.Value.Year < 2000) throw new InvalidOperationException("Yürürlük tarihi gerekiyor.");
            var policy = await db.LeavePolicySettings.SingleOrDefaultAsync(x => x.EffectiveFrom == effective.Value.Date);
            if (policy == null) { policy = new LeavePolicySetting { EffectiveFrom = effective.Value.Date }; db.LeavePolicySettings.Add(policy); }
            policy.CountSaturday = saturday;
        }
        // Preview writes are transaction-local and always rolled back by PreviewAsync.
        await db.SaveChangesAsync();
        var now = LeaveAccountingRules.Now;
        var candidates = await db.Leaves.Include(x => x.LeaveType).Where(x => x.StartDate > now &&
            (x.Status == 0 || x.Status == 1 || x.Status == 4)).OrderBy(x => x.Id).ToListAsync();
        var rows = new List<PolicyChange>();
        foreach (var leave in candidates.Where(x => year.HasValue ? x.StartDate.Year <= year && x.EndDate.Year >= year : x.EndDate >= effective))
        {
            if (await accounting.CalendarErrorAsync(leave.StartDate, leave.EndDate) != null) continue;
            rows.Add(new PolicyChange(leave.Id, leave.EmployeeId, leave.Status, leave.RequestedDays,
                await accounting.CalculateDaysAsync(leave.StartDate, leave.EndDate), LeaveAccountingService.IsAnnual(leave)));
        }
        var calendars = await db.LeaveCalendars.Where(x => x.Year > 0).OrderBy(x => x.Year)
            .Select(x => new { x.Year, x.Revision, x.PublishedJson }).ToListAsync();
        var policies = await db.LeavePolicySettings.OrderBy(x => x.EffectiveFrom).Select(x => new { x.EffectiveFrom, x.CountSaturday }).ToListAsync();
        var accounts = await db.LeaveAccounts.OrderBy(x => x.EmployeeId).Select(x => new { x.EmployeeId, x.Revision }).ToListAsync();
        return new PolicyPreview(year, effective, saturday, rows,
            LeaveAccountingService.Hash(JsonSerializer.Serialize(new { year, effective, saturday, Date = LeaveAccountingService.Today, rows, calendars, policies, accounts })));
    }
    public async Task PublishAsync(int? year, DateTime? effective, bool saturday, string token, string actor)
    {
        await accounting.RequireAdminAsync(actor);
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            await accounting.LockPolicyAsync();
            var preview = await BuildPreviewAsync(year, effective, saturday);
            if (token != preview.Token) throw new InvalidOperationException("Kayıtlar değişti. Yeniden önizleme yapın.");
            foreach (var row in preview.Changes)
            {
                var employee = (await db.EmployeePortals.FromSqlInterpolated($"SELECT * FROM employee_portal WHERE Id = {row.EmployeeId} FOR UPDATE").ToListAsync()).Single();
                var account = await accounting.EnsureAccountAsync(employee);
                var leave = await db.Leaves.SingleAsync(x => x.Id == row.LeaveId);
                if (row.Status == 1 && row.Annual && row.Before != row.After)
                {
                    var charge = await db.LeaveCharges.SingleAsync(x => x.LeaveId == row.LeaveId);
                    await accounting.MoveAsync(employee, account, charge.Days - row.After, "PolicyCorrection",
                        "policy:" + Guid.NewGuid(), actor, "Başlamamış izin için takvim/cumartesi kuralı düzeltmesi", LeaveAccountingService.Today, leave.Id);
                    charge.Days = row.After;
                }
                leave.RequestedDays = row.After; leave.UpdateDate = DateTime.UtcNow;
                await accounting.CaptureCalculationAsync(leave);
            }
            if (year.HasValue)
            {
                var calendar = await db.LeaveCalendars.SingleAsync(x => x.Year == year);
                calendar.Revision++; calendar.ApprovedBy = actor; calendar.UpdateDate = DateTime.UtcNow;
                db.LeaveCalendarVersions.Add(new LeaveCalendarVersion { Year = year.Value, Revision = calendar.Revision,
                    Actor = actor, SourceUrl = calendar.SourceUrl, HolidaysJson = calendar.PublishedJson, CreatedDate = DateTime.UtcNow });
            }
            await db.SaveChangesAsync(); await tx.CommitAsync();
        }
        catch { await tx.RollbackAsync(); db.ChangeTracker.Clear(); throw; }
    }
}
public record PolicyChange(int LeaveId, int EmployeeId, int Status, decimal Before, decimal After, bool Annual);
public record PolicyPreview(int? Year, DateTime? EffectiveFrom, bool CountSaturday, List<PolicyChange> Changes, string Token);
