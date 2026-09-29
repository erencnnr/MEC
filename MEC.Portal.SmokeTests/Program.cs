using System.Security.Claims;
using MEC.Application.Abstractions.Service.ApprovalWorkflowService;
using MEC.Application.Abstractions.Service.ApprovalWorkflowService.Model;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Portal.Controllers;
using MEC.Portal.Models;
using MEC.Portal.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

if (args.Contains("--check-test-schema") || args.Contains("--apply-test-schema"))
{
    await TestSchemaMaintenance.RunAsync(args.Contains("--apply-test-schema"));
    return;
}

if (TurkeyTime.GetDate(new DateTime(2026, 9, 26, 20, 59, 59, DateTimeKind.Utc)) != new DateTime(2026, 9, 26)
    || TurkeyTime.GetDate(new DateTime(2026, 9, 26, 21, 0, 0, DateTimeKind.Utc)) != new DateTime(2026, 9, 27))
    throw new InvalidOperationException("Annual leave date must change at midnight in Turkey.");
Console.WriteLine("PASS: Annual leave job resolves the native time zone and Turkey midnight correctly.");
var actorProvider = new TestActors();
// Resolve the async method against the EF assemblies actually loaded by the portal.
// A normal build alone does not detect incompatible EF references across projects.
var assignmentMethod = typeof(MEC.Application.Service.SchoolService.SchoolManagementService)
    .GetMethod("AssignManagerAsync")!;
var assignmentStateMachine = System.Reflection.CustomAttributeExtensions
    .GetCustomAttribute<System.Runtime.CompilerServices.AsyncStateMachineAttribute>(assignmentMethod)!.StateMachineType;
var assignmentMoveNext = assignmentStateMachine.GetMethod("MoveNext",
    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)!;
System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(assignmentMoveNext.MethodHandle);
Console.WriteLine("PASS: School manager assignment resolves against the portal EF runtime.");
var cookieEvents = new PortalCookieEvents(actorProvider);
foreach (var isAdmin in new[] { true, false })
{
    actorProvider.Actor.IsAdministrator = isAdmin;
    var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "test@mec.local") }, "Cookies");
    if (!isAdmin) identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
    var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), new AuthenticationProperties(), "Cookies");
    var context = new CookieValidatePrincipalContext(new DefaultHttpContext(),
        new AuthenticationScheme("Cookies", null, typeof(CookieAuthenticationHandler)), new CookieAuthenticationOptions(), ticket);
    await cookieEvents.ValidatePrincipal(context);
    if (context.Principal!.IsInRole("Admin") != isAdmin || !context.ShouldRenew)
        throw new InvalidOperationException("Existing cookie must pick up role grants and revocations.");
}
Console.WriteLine("PASS: Existing cookie refreshes granted and revoked admin roles.");
await PortalUserFormChecks.RunAsync();
if (!args.Contains("--serve-ui")) return;

// This separate test host has no database, production authentication, or write endpoints.
var portalPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../MEC.Portal"));
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ApplicationName = typeof(AccountController).Assembly.GetName().Name,
    ContentRootPath = portalPath,
    WebRootPath = Path.Combine(portalPath, "wwwroot"),
    EnvironmentName = "Development"
});
builder.Services.AddControllersWithViews();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
var app = builder.Build();
app.UseStaticFiles();
app.MapGet("/preview/{page}", async (HttpContext http, string page) =>
{
    var samples = new Dictionary<string, (string View, object Model)>
    {
        ["agreements"] = ("/Views/Admin/LeaveAgreement.cshtml", new AdminLeaveAgreementListViewModel
        {
            TotalCount = 3,
            Items = Enumerable.Range(1, 3).Select(i => new AdminLeaveAgreementViewModel
            {
                Id = i, FirstName = "Örnek Uzun Çalışan Adı", LastName = "Uzun Soyadı", Email = "ornek.uzun.adres@mecokullari.k12.tr",
                PhoneNumber = "0555 123 45 67", BalanceAsOfDate = new DateTime(2026, 9, 1),
                AgreedLeaveDays = 12, CurrentYearEarnedDays = 14, CurrentYearUsedDays = 3,
                CurrentBalance = -2, CreatedDate = new DateTime(2026, 9, 26)
            }).ToList()
        }),
        ["schools"] = ("/Views/SchoolSettings/Index.cshtml", new SchoolManagementModel
        {
            Schools = new() { new(1, "Koşuyolu", 1), new(2, "Bahçeköy", 2), new(3, "Örnek Okul 3", null), new(4, "Örnek Okul 4", null) },
            Users = new() { new(1, "Örnek Okul Müdürü (mudur@mec.local)"), new(2, "İkinci Müdür (ikinci@mec.local)") }
        }),
        ["request"] = ("/Views/Leave/RequestLeave.cshtml", new LeaveRequestViewModel
        {
            RemainingLeaveDays = -2, LeaveTypes = new() { new() { Id = 1, Code = "ANNUAL", Name = "Yıllık İzin" } }
        }),
        ["user"] = ("/Views/Admin/PortalUserDetail.cshtml", new AdminPortalUserEditViewModel
        {
            Id = 1, FirstName = "Örnek", LastName = "Çalışan", Email = "ornek@mec.local", PhoneNumber = "05551234567",
            LeaveDays = -2, LocationOptions = new() { new("Koşuyolu", "1"), new("Bahçeköy", "2") }
        })
    };
    samples["balances"] = ("/Views/Admin/LeaveBalances.cshtml", new AdminLeaveBalanceListViewModel
    {
        CanViewAllLocations = true, TotalCount = 1,
        Items = new() { new() { EmployeePortalId = 1, EmployeeName = "Örnek Uzun Çalışan Adı",
            Email = "uzun.adres@mecokullari.k12.tr", LocationNames = "Koşuyolu, Bahçeköy",
            HireDate = new DateTime(2020, 9, 27), NextEntitlementDate = new DateTime(2026, 9, 27),
            CompletedServiceYears = 5, AnnualEntitlementDays = 14, CurrentBalance = -2 } }
    });
    samples["requests"] = ("/Views/Admin/LeaveRequests.cshtml", new AdminLeaveRequestListViewModel
    {
        TotalCount = 1, TotalPages = 1,
        Items = new() { new() { Id = 1, EmployeeName = "Örnek Uzun Çalışan Adı",
            LocationNames = "Koşuyolu, Bahçeköy", LeaveType = "Yıllık İzin", RequestedDays = 3,
            StartDate = new DateTime(2026, 9, 28), EndDate = new DateTime(2026, 9, 30),
            StatusLabel = "Okul Müdürü Onayı Bekliyor", CanTakeAction = true } }
    });
    samples["settings"] = ("/Views/AdminSettings/Index.cshtml", new object());
    samples["account"] = ("/Views/LeaveAccounts/Account.cshtml", new MEC.Application.Service.LeaveService.AccountPage {
        Employee=new MEC.Domain.Entity.Employee.EmployeePortal{Id=1,FirstName="Örnek",LastName="Çalışan",HireDate=new DateTime(2025,9,28)},
        Account=new MEC.Domain.Entity.Leave.LeaveAccount{Balance=28,NeedsReview=true},
        Movements=[new(){EffectiveDate=new DateTime(2026,9,28),Days=14,Kind="AnnualAccrual",Actor="annual-job",Reason="1. çalışma yılı hak edişi"}]
    });
    samples["calendar"]=("/Views/LeaveAccounts/Calendar.cshtml",new MEC.Portal.Controllers.CalendarEditPage(2027,[],MEC.Application.Service.LeaveService.LeaveCalendarService.FixedDays(2027)));
    samples["calendar-import-error"] = samples["calendar"];
    samples["policy"]=("/Views/LeaveAccounts/Policy.cshtml",new object());
    samples["policy-preview"]=("/Views/LeaveAccounts/PolicyPreview.cshtml",new MEC.Application.Service.LeaveService.PolicyPreview(2027,null,false,[new(1,1,1,3,2,true)],"preview"));
    samples["jobs"]=("/Views/LeaveAccounts/Jobs.cshtml",new List<MEC.Domain.Entity.Leave.LeaveJobResult>{new(){EmployeeId=1,BusinessDate=new DateTime(2026,9,28),Success=false,Error="İşe giriş eksik"}});
    var cancellationExample = new MEC.Application.Service.LeaveService.CancellationListItem {
        Id=1, LeaveId=1, EmployeeId=1, EmployeeName="Örnek Çalışan", Email="ornek@mec.local", RequestedBy="ornek@mec.local",
        LeaveType="Yıllık İzin", Days=3, StartDate=new DateTime(2026,10,12,9,0,0), EndDate=new DateTime(2026,10,14,18,0,0),
        RequestedAt=new DateTime(2026,9,29,10,30,0), Reason="Seyahat planım değiştiği için iznimi iptal etmek istiyorum." };
    samples["cancellations"]=("/Views/LeaveCancellations/Index.cshtml",new MEC.Application.Service.LeaveService.CancellationListPage { TotalCount=1, Items=[cancellationExample] });
    samples["cancellation-detail"]=("/Views/LeaveCancellations/Detail.cshtml",new CancellationDetailViewModel { Item=cancellationExample });
    samples["imports"]=("/Views/LeaveAccounts/Imports.cshtml",new List<MEC.Domain.Entity.Leave.LeaveImportBatch>{new(){Actor="admin@mec.local",CreatedDate=DateTime.UtcNow,ResultJson=System.Text.Json.JsonSerializer.Serialize(new MEC.Application.Abstractions.Service.LeaveService.Model.BulkLeaveUploadResultModel{Message="1 kullanıcıya gün eklendi.",UpdatedUsers=[new(){Email="ornek@mec.local",NewLeaveDays=28}]})}});
    samples["login"] = ("/Views/Account/Login.cshtml", new LoginViewModel());
    if (!samples.TryGetValue(page, out var sample)) return Results.NotFound();
    http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.Name, "preview@mec.local"), new Claim(ClaimTypes.Role, http.Request.Query["role"].FirstOrDefault() ?? "Admin")
    }, "Preview"));
    var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
    var engine = http.RequestServices.GetRequiredService<IRazorViewEngine>();
    var view = engine.GetView(null, sample.View, true);
    if (!view.Success) throw new InvalidOperationException(string.Join(",", view.SearchedLocations));
    using var writer = new StringWriter();
    var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = sample.Model };
    var tempData = new TempDataDictionary(http, http.RequestServices.GetRequiredService<ITempDataProvider>());
    if (page == "calendar-import-error")
    {
        tempData["AccountingError"] = true;
        tempData["AccountingMessage"] = "Diyanet erişimi reddetti (403); takvim aktarılmadı. Tatilleri elle girebilirsiniz.";
    }
    await view.View.RenderAsync(new ViewContext(actionContext, view.View, viewData, tempData, writer, new HtmlHelperOptions()));
    return Results.Content(writer.ToString(), "text/html");
});
// Preview-only destination for checking list row navigation, without live controllers or database writes.
app.MapGet("/Admin/LeaveCancellations/{id:int}", () => Results.Redirect("/preview/cancellation-detail"));
app.Run("http://127.0.0.1:5187");

sealed class TestActors : IApprovalWorkflowService
{
    public ApprovalActorModel Actor { get; } = new() { Email = "test@mec.local" };
    public Task<ApprovalActorModel?> GetActorAsync(string email) => Task.FromResult<ApprovalActorModel?>(Actor);
    public Task<ApprovalRouteModel?> ResolveRouteAsync(int employeePortalId) => throw new NotSupportedException();
    public Task<Dictionary<int, ApprovalRouteModel>> ResolveRoutesAsync(IEnumerable<int> employeePortalIds) => throw new NotSupportedException();
}
