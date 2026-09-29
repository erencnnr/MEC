using System.Text.Json;
using MEC.Application.Service.LeaveService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MEC.Portal.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/LeaveAccounts")]
[AutoValidateAntiforgeryToken]
public class LeaveAccountsController(LeaveAccountingService accounting, LeaveCalendarService calendars, IWebHostEnvironment environment) : Controller
{
    private string Actor => User.Identity?.Name ?? "";
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Account(int id) => View(await accounting.GetAccountPageAsync(id, Actor));

    [HttpPost("{id:int}/Manual")]
    public async Task<IActionResult> Manual(int id, [ModelBinder(BinderType=typeof(MEC.Portal.Models.LeaveDayModelBinder))] decimal days, string reason, string operationId)
        => await ExecuteAsync(() => accounting.ManualAsync(id, days, reason, Actor, operationId), $"/Admin/LeaveAccounts/{id}");

    [HttpPost("{id:int}/PreviewDates")]
    public async Task<IActionResult> PreviewDates(int id, DateTime hireDate, DateTime? birthDate)
    {
        try
        {
            if (!ModelState.IsValid) throw new InvalidOperationException("Tarihleri kontrol edin.");
            var preview = await accounting.PreviewDatesAsync(id, hireDate, birthDate, Actor);
            var page = await accounting.GetAccountPageAsync(id, Actor); page.Preview = preview;
            return View("Account", page);
        }
        catch (InvalidOperationException ex) { TempData["AccountingMessage"] = ex.Message; return Redirect($"/Admin/LeaveAccounts/{id}"); }
    }
    [HttpPost("{id:int}/ApplyDates")]
    public async Task<IActionResult> ApplyDates(int id, DateTime hireDate, DateTime? birthDate, string token, string reason)
        => await ExecuteAsync(() => accounting.CorrectDatesAsync(id, hireDate, birthDate, token, reason, Actor), $"/Admin/LeaveAccounts/{id}");

    [HttpPost("{id:int}/Reconcile")]
    public async Task<IActionResult> Reconcile(int id, [ModelBinder(BinderType=typeof(MEC.Portal.Models.LeaveDayModelBinder))] decimal balance, DateTime cutoff, DateTime? hireDate, DateTime? birthDate, string reason, bool reactivate)
        => await ExecuteAsync(() => accounting.SaveAgreementAsync(id, balance, cutoff, 0, 0, false, Actor, hireDate, birthDate, reason, reactivate), $"/Admin/LeaveAccounts/{id}");

    [HttpGet("{id:int}/Versions/{versionId:int}/Pdf")]
    public async Task<IActionResult> VersionPdf(int id, int versionId)
    {
        var page = await accounting.GetAccountPageAsync(id, Actor);
        var version = page.Versions.SingleOrDefault(x => x.Id == versionId);
        if (version == null) return NotFound();
        using var json = JsonDocument.Parse(version.Snapshot);
        var name = json.RootElement.GetProperty("AgreementPdfFileName").GetString();
        if (string.IsNullOrEmpty(name) || Path.GetFileName(name) != name) return NotFound();
        var file = Path.Combine(environment.WebRootPath, "uploads", "leave-agreements", name);
        return System.IO.File.Exists(file) ? PhysicalFile(file, "application/pdf") : NotFound();
    }
    [HttpGet("Imports")]
    public async Task<IActionResult> Imports() => View(await accounting.GetImportsAsync(Actor));
    [HttpGet("Jobs")]
    public async Task<IActionResult> Jobs() => View(await accounting.GetJobResultsAsync(Actor));

    [HttpGet("Calendar")]
    public async Task<IActionResult> Calendar(int? year)
    {
        var all = await calendars.ListAsync(Actor);
        var selected = year ?? LeaveAccountingService.Today.Year;
        if (selected < 2000 || selected > LeaveAccountingService.Today.Year + 10) return BadRequest();
        var item = all.SingleOrDefault(x => x.Year == selected);
        var days = item == null ? LeaveCalendarService.FixedDays(selected) : JsonSerializer.Deserialize<List<CalendarDay>>(item.DraftJson) ?? [];
        return View(new CalendarEditPage(selected, all, days));
    }
    [HttpPost("Calendar/Import")]
    public async Task<IActionResult> Import(int year)
        => await ExecuteAsync(() => calendars.ImportAsync(year, Actor), $"/Admin/LeaveAccounts/Calendar?year={year}#calendar-message",
            $"{year} takvimi getirildi: sabit tatiller, 7 bayram günü ve 2 arife taslağa kaydedildi. Tarihleri kontrol edip ‘Kontrol ettim, etkisini göster’ ile onaylayın.");
    [HttpPost("Calendar/Save")]
    public async Task<IActionResult> SaveCalendar(int year, List<CalendarDayInput> days)
        => await ExecuteAsync(() => calendars.SaveDraftAsync(year, JsonSerializer.Serialize(days.Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .Select(x => new CalendarDay(x.Name!, x.Start, x.End))), "Admin girişi", Actor), $"/Admin/LeaveAccounts/Calendar?year={year}");

    [HttpGet("Policy")]
    public IActionResult Policy() => View();
    [HttpPost("PolicyPreview")]
    public async Task<IActionResult> PolicyPreview(int? year, DateTime? effectiveFrom, bool countSaturday)
    {
        try
        {
            if (!ModelState.IsValid) throw new InvalidOperationException("Alanları kontrol edin.");
            return View(await calendars.PreviewAsync(year, effectiveFrom, countSaturday, Actor));
        }
        catch (InvalidOperationException ex) { TempData["AccountingError"] = true; TempData["AccountingMessage"] = ex.Message; return Redirect(year.HasValue ? $"/Admin/LeaveAccounts/Calendar?year={year}" : "/Admin/LeaveAccounts/Policy"); }
    }
    [HttpPost("PolicyPublish")]
    public async Task<IActionResult> PolicyPublish(int? year, DateTime? effectiveFrom, bool countSaturday, string token)
        => await ExecuteAsync(() => calendars.PublishAsync(year, effectiveFrom, countSaturday, token, Actor),
            year.HasValue ? $"/Admin/LeaveAccounts/Calendar?year={year}" : "/Admin/LeaveAccounts/Policy");

    [HttpPost("ReviewHistorical")]
    public async Task<IActionResult> ReviewHistorical(int leaveId, bool included, string reason)
        => await ExecuteAsync(() => accounting.ReviewHistoricalAsync(leaveId, included, reason, Actor), $"/Admin/LeaveRequests/{leaveId}");

    [HttpPost("ReviewSplit")]
    public async Task<IActionResult> ReviewSplit(int leaveId, DateTime cutoff, [ModelBinder(BinderType=typeof(MEC.Portal.Models.LeaveDayModelBinder))] decimal before, string reason)
        => await ExecuteAsync(() => accounting.ReviewSplitAsync(leaveId, cutoff, before, reason, Actor), $"/Admin/LeaveRequests/{leaveId}");

    private async Task<IActionResult> ExecuteAsync(Func<Task> action, string destination, string successMessage = "İşlem tamamlandı.")
    {
        try
        {
            if (!ModelState.IsValid) throw new InvalidOperationException("Gönderilen alanları kontrol edin.");
            await action(); TempData["AccountingMessage"] = successMessage;
            TempData["AccountingError"] = false;
        }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException or HttpRequestException or TaskCanceledException)
        {
            TempData["AccountingError"] = true;
            TempData["AccountingMessage"] = ex switch
            {
                TaskCanceledException => "Diyanet yanıt vermedi; takvim aktarılmadı. Tekrar deneyin veya tatilleri elle girin.",
                HttpRequestException { StatusCode: System.Net.HttpStatusCode.Forbidden } => "Diyanet erişimi reddetti (403); takvim aktarılmadı. Tatilleri elle girebilirsiniz.",
                HttpRequestException httpError => $"Diyanet kaynağına erişilemedi{(httpError.StatusCode.HasValue ? $" (HTTP {(int)httpError.StatusCode.Value})" : "")}; takvim aktarılmadı. Tekrar deneyin veya tatilleri elle girin.",
                _ => ex.Message
            };
        }
        return Redirect(destination);
    }
}
public record CalendarEditPage(int Year, List<MEC.Domain.Entity.Leave.LeaveCalendar> Calendars, List<CalendarDay> Days);
public class CalendarDayInput
{
    public string? Name { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}
