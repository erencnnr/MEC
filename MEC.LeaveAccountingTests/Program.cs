using System.Text.RegularExpressions;
using System.Text.Json;
using MEC.Application.Service.LeaveService;
using MEC.Application.Abstractions.Service.ApprovalWorkflowService;
using MEC.Application.Abstractions.Service.ApprovalWorkflowService.Model;
using MEC.DAL.Config.Contexts;
using MEC.Domain.Common;
using MEC.Domain.Entity.Employee;
using MEC.Domain.Entity.Leave;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;

var baseOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseMySql("Server=localhost;Database=mec_schema_only;User=root", new MySqlServerVersion(new Version(8,0,36))).Options;
if (args.Length == 2 && args[0] == "--official-calendar")
{
    using var http = new HttpClient();
    using var context = new ApplicationDbContext(baseOptions);
    var service = new LeaveCalendarService(http, context, new LeaveAccountingService(context, new TestWorkflow()));
    var result = await service.ReadOfficialAsync(int.Parse(args[1]));
    Console.WriteLine($"PASS: Live Diyanet import {args[1]}: {result.Days.Count} holidays; source {result.SourceUrl}. No database changes.");
    return;
}
// Read-only check against a downloaded official page; never connects to a database.
if (args.Length == 3 && args[0] == "--parse-calendar")
{
    var calendar = LeaveCalendarService.ParseDiyanet(await File.ReadAllTextAsync(args[2]), int.Parse(args[1]));
    LeaveCalendarService.ValidateComplete(calendar, int.Parse(args[1]));
    Console.WriteLine($"PASS: Official calendar {args[1]}: {calendar.Count} holidays, including both arifes.");
    return;
}
if (args.Length == 2 && args[0] == "--generate-schema")
{
    using var model = new ApplicationDbContext(baseOptions);
    var names = new[] { "leave_account", "leave_movement", "leave_accrual", "leave_charge", "leave_agreement_version",
        "leave_cancellation", "leave_import_batch", "leave_import_row", "leave_job_result", "leave_calendar", "leave_calendar_version" };
    var ddl = model.Database.GenerateCreateScript();
    var statements = ddl.Split(';').Where(x => names.Any(name => x.Contains("`" + name + "`")));
    var output = "-- Stop the Portal and annual job; back up the database first.\n-- Additive, repeatable schema installation. Existing balances are not altered.\n";
    foreach (var statement in statements)
    {
        var text = statement.Trim();
        if (text.StartsWith("CREATE TABLE")) output += text.Replace("CREATE TABLE", "CREATE TABLE IF NOT EXISTS") + ";\n\n";
        else if (text.StartsWith("CREATE") && text.Contains("INDEX"))
        {
            var match = Regex.Match(text, @"INDEX `([^`]+)` ON `([^`]+)`");
            var index = match.Groups[1].Value; var table = match.Groups[2].Value;
            output += $"SET @ddl = IF(EXISTS(SELECT 1 FROM information_schema.statistics WHERE table_schema=DATABASE() AND table_name='{table}' AND index_name='{index}'), 'SELECT 1', '{text.Replace("'", "''")}');\nPREPARE migration_statement FROM @ddl;\nEXECUTE migration_statement;\nDEALLOCATE PREPARE migration_statement;\n\n";
        }
    }
    output += "INSERT INTO leave_calendar (Year,Revision,IsApproved,ApprovedBy,SourceUrl,DraftJson,PublishedJson,CreatedDate) SELECT 0,0,0,'','','[]','[]',UTC_TIMESTAMP() WHERE NOT EXISTS(SELECT 1 FROM leave_calendar WHERE Year=0);\n";
    await File.WriteAllTextAsync(args[1], output);
    Console.WriteLine("Generated additive accounting schema."); return;
}

static void Check(bool value, string message) { if (!value) throw new Exception(message); Console.WriteLine("PASS: " + message); }
var hire = new DateTime(2025,9,28);
Check(LeaveAccountingService.ExpectedTotal(hire, null, 0, new DateTime(2026,9,28)) == 14, "First anniversary gives 14");
Check(LeaveAccountingService.ExpectedTotal(hire, null, 0, new DateTime(2027,9,28)) == 28, "Unused days carry: 14 to 28");
Check(LeaveAccountingService.ExpectedTotal(hire, null, 0, new DateTime(2028,9,28)) == 42, "Unused days carry: 28 to 42");
Check(LeaveAccountingService.ExpectedTotal(hire, null, 0, new DateTime(2028,9,28)) - 5 == 37, "Carry after five days used is 37");
Check(LeaveAccountingService.ExpectedTotal(hire, null, 0, new DateTime(2027,1,1)) == 14, "January 1 does not reset or accrue");
Check(LeaveAccountingRules.DaysAfterCutoff(1.5m,1m) == .5m, "Cutoff split preserves original rounded total");
Check(!LeaveAccountingRules.Overlaps(hire.AddHours(9),hire.AddHours(12),hire.AddHours(12),hire.AddHours(18)), "Adjacent hours do not overlap");
Check(LeaveAccountingRules.Overlaps(hire.AddHours(9),hire.AddHours(12),hire.AddHours(11),hire.AddHours(18)), "Overlapping hours are detected");
Check(MEC.Portal.Models.LeaveDayModelBinder.TryParse("0.5", out var dotDay) && dotDay==0.5m &&
    MEC.Portal.Models.LeaveDayModelBinder.TryParse("-0,5", out var commaDay) && commaDay==-0.5m,"Turkish and HTML decimal days are parsed without thousands conversion");
Check(LeaveAccountingService.ImportFingerprint([("a@test.local",2m,"ek"),("b@test.local",3m,"ek")]) ==
    LeaveAccountingService.ImportFingerprint([("B@test.local",3.00m,"ek"),(" A@test.local ",2.0m,"ek")]),
    "Excel content fingerprint ignores row order, casing and numeric formatting");
var incompleteRejected = false;
try { LeaveCalendarService.ParseDiyanet("<table></table>",2027); } catch (InvalidOperationException) { incompleteRejected = true; }
Check(incompleteRejected, "Incomplete holiday import is rejected");
var html = "<table>";
foreach (var (day,month,name) in new[] { (8,"MART","AREFE"),(9,"MART","RAMAZAN BAYRAMI (1. Gün)"),(10,"MART","RAMAZAN BAYRAMI (2. Gün)"),(11,"MART","RAMAZAN BAYRAMI (3. Gün)"),
    (15,"MAYIS","AREFE"),(16,"MAYIS","KURBAN BAYRAMI (1. Gün)"),(17,"MAYIS","KURBAN BAYRAMI (2. Gün)"),(18,"MAYIS","KURBAN BAYRAMI (3. Gün)"),(19,"MAYIS","KURBAN BAYRAMI (4. Gün)") })
    html += $"<tr><td>1</td><td>AY</td><td>1448</td><td>{day}</td><td>{month}-2027</td><td>GÜN</td><td>{name}</td></tr>";
html += "<tr><td>1</td><td>AY</td><td>1448</td><td>4</td><td>OCAK-2027</td><td>GÜN</td><td>MİRAC KANDİLİ</td></tr></table>";
var imported = LeaveCalendarService.ParseDiyanet(html,2027);
Check(imported.Count == 17 && imported.Count(x=>x.Start.Hour==13)==3, "Importer includes fixed holidays and arifes, excludes kandils");
LeaveCalendarService.ValidateComplete(imported, 2027);
Check(LeaveCalendarService.FindOfficialUrl("<a href=icerik.php?icerik=158>2026 Yılı Resmi Tatiller</a><a href=dinigunler.php?yil=2026>2026 Yılı Dini Günler</a>",2026)
    == "https://vakithesaplama.diyanet.gov.tr/dinigunler.php?yil=2026", "Unquoted current Diyanet links select religious list, not official-holiday table");
Check(LeaveCalendarService.FindOfficialUrl("<a href = 'icerik.php?icerik=154'><span>2027 Yılı Dini Günler</span></a>",2027)
    == "https://vakithesaplama.diyanet.gov.tr/icerik.php?icerik=154", "Quoted legacy Diyanet links remain supported");
var foreignLinkRejected = false;
try { LeaveCalendarService.FindOfficialUrl("<a href='https://example.com/icerik.php?icerik=154'>2027 Yılı Dini Günler</a>",2027); }
catch (InvalidOperationException) { foreignLinkRejected = true; }
Check(foreignLinkRejected, "Calendar importer rejects links outside the official source");
using (var handler = new OfficialCalendarHandler(html))
using (var client = new HttpClient(handler))
using (var context = new ApplicationDbContext(baseOptions))
{
    var service = new LeaveCalendarService(client, context, new LeaveAccountingService(context, new TestWorkflow()));
    var official = await service.ReadOfficialAsync(2027);
    Check(handler.RequestCount == 2 && official.Days.Count == 17,
        "Official fetch identifies the portal on both HTTP requests and validates the full calendar before saving");
}
var route = typeof(MEC.Portal.Controllers.LeaveController).GetMethod("BulkLeaveUpload")!;
Check(route.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
    .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Any(x=>x.Roles=="Admin"), "Excel route requires Admin");

if (!args.Contains("--mysql")) { Console.WriteLine("SKIP: Real MySQL concurrency tests require --mysql and MEC_LEAVE_TEST_MYSQL."); return; }
var configured = Environment.GetEnvironmentVariable("MEC_LEAVE_TEST_MYSQL");
if (string.IsNullOrWhiteSpace(configured)) throw new InvalidOperationException("Set MEC_LEAVE_TEST_MYSQL to an isolated MySQL test server. Production config is never read.");
var connection = new MySqlConnectionStringBuilder(configured);
var database = "mec_leave_test_" + Guid.NewGuid().ToString("N");
connection.Database = database;
var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseMySql(connection.ConnectionString, new MySqlServerVersion(new Version(8,0,36))).Options;
var actors = new TestWorkflow();
async Task WithService(Func<LeaveAccountingService,ApplicationDbContext,Task> action)
{ await using var ctx = new ApplicationDbContext(options); await action(new LeaveAccountingService(ctx,actors),ctx); }
await using var setup = new ApplicationDbContext(options);
try
{
    await setup.Database.EnsureCreatedAsync();
    var migrationPath=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../MEC.Portal/DatabaseScripts/mysql_leave_accounting.sql"));
    await using (var migrationConnection=new MySqlConnection(new MySqlConnectionStringBuilder(connection.ConnectionString){AllowUserVariables=true}.ConnectionString))
    {
        await migrationConnection.OpenAsync();
        for(var repetition=0;repetition<2;repetition++)
        {
            await using var migrationCommand=new MySqlCommand(await File.ReadAllTextAsync(migrationPath),migrationConnection);
            migrationCommand.CommandTimeout=120;await migrationCommand.ExecuteNonQueryAsync();
        }
    }
    Check(await setup.LeaveCalendars.CountAsync(x=>x.Year==0)==1,"Schema migration can be reapplied without duplicating the policy gate");
    for(var year=LeaveAccountingService.Today.Year-2;year<=LeaveAccountingService.Today.Year+3;year++)
        setup.LeaveCalendars.Add(new LeaveCalendar { Year=year, IsApproved=true, DraftJson="[]", PublishedJson="[]" });
    var person = new EmployeePortal { FirstName="Test", LastName="Employee", Email="employee@test.local",
        HireDate=LeaveAccountingService.Today.AddYears(-1), AnnualLeaveProcessedThrough=LeaveAccountingService.Today.AddDays(-1), LeaveDays=0 };
    setup.EmployeePortals.Add(person);
    var annual = new LeaveType { Name="Yıllık", Code=LeaveTypeCodes.Annual, IsActive=true };
    setup.LeaveTypes.Add(annual); await setup.SaveChangesAsync();
    var id = person.Id;
    await Task.WhenAll(Enumerable.Range(0,8).Select(_ => WithService(async (svc,_) => { await svc.RefreshAsync(id,LeaveAccountingService.Today); })));
    setup.ChangeTracker.Clear();
    Check((await setup.EmployeePortals.FindAsync(id))!.LeaveDays==14,"Eight concurrent jobs credit one anniversary once");
    await WithService(async(svc,_)=>await svc.ManualAsync(id,5,"Test grant","admin@test.local",Guid.NewGuid().ToString()));
    await WithService(async(svc,_)=>{await svc.RefreshAsync(id,LeaveAccountingService.Today.AddYears(1));});
    setup.ChangeTracker.Clear();
    Check((await setup.EmployeePortals.FindAsync(id))!.LeaveDays==33,"Manual five days survive next annual accrual");
    // Use a separate new account to test corrections without a future-dated test checkpoint.
    var correctionPerson = new EmployeePortal { Email="correction@test.local", HireDate=LeaveAccountingService.Today.AddYears(-1), AnnualLeaveProcessedThrough=LeaveAccountingService.Today.AddDays(-1) };
    setup.EmployeePortals.Add(correctionPerson); await setup.SaveChangesAsync();
    await WithService(async(svc,_)=>{ await svc.RefreshAsync(correctionPerson.Id,LeaveAccountingService.Today);
        var date=correctionPerson.HireDate!.Value.AddDays(7);var preview=await svc.PreviewDatesAsync(correctionPerson.Id,date,null,"admin@test.local");
        await svc.CorrectDatesAsync(correctionPerson.Id,date,null,preview.Token,"Correct hire date","admin@test.local");
        await svc.RefreshAsync(correctionPerson.Id,LeaveAccountingService.Today.AddDays(7)); });
    setup.ChangeTracker.Clear();
    Check((await setup.EmployeePortals.FindAsync(correctionPerson.Id))!.LeaveDays==14,"Moved anniversary does not double the year's right");
    var key="test-batch";
    await Task.WhenAll(Enumerable.Range(0,5).Select(_=>WithService(async(svc,_)=>{await svc.ImportRowAsync(id,2,"Import","admin@test.local",key,"employee@test.local");})));
    setup.ChangeTracker.Clear(); Check((await setup.EmployeePortals.FindAsync(id))!.LeaveDays==35,"Concurrent duplicate Excel rows credit once");
    var unauthorized=false;try{await WithService(async(svc,_)=>await svc.ManualAsync(id,100,"Forbidden","employee@test.local",Guid.NewGuid().ToString()));}catch(UnauthorizedAccessException){unauthorized=true;}
    Check(unauthorized,"Non-admin accounting writes are rejected");
    var start=LeaveAccountingService.Today.AddDays(10).AddHours(9);while(start.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) start=start.AddDays(1);
    var leave=new Leave { EmployeeId=id, LeaveTypeId=annual.Id, StartDate=start, EndDate=start.AddHours(9), RequestedDays=1, Status=4, Reason="Test" };
    setup.Leaves.Add(leave);await setup.SaveChangesAsync();
    await Task.WhenAll(Enumerable.Range(0,5).Select(_=>WithService(async(svc,_)=>{await svc.SetStatusAsync(leave.Id,1,"admin@test.local");})));
    setup.ChangeTracker.Clear(); Check((await setup.EmployeePortals.FindAsync(id))!.LeaveDays==34,"Duplicate approvals debit once");
    await WithService(async(svc,_)=>await svc.RequestCancellationAsync(leave.Id,"Will work instead","employee@test.local"));
    await WithService(async(svc,_)=>{
        var pending = await svc.GetCancellationsAsync("admin@test.local", new() { Search="Test Employee", From=start.Date, To=start.Date });
        Check(pending.TotalCount==1 && pending.Items.Single().LeaveId==leave.Id && pending.Items[0].EmployeeName=="Test Employee",
            "Cancellation list joins employee names and filters by name and inclusive start dates");
        Check((await svc.GetCancellationsAsync("admin@test.local", new(){Search="employee@test.local"})).TotalCount==1,
            "Cancellation search supports employee email");
        Check((await svc.GetCancellationsAsync("admin@test.local", new(){From=start.AddDays(1)})).TotalCount==0,
            "Cancellation date filter excludes earlier leave starts");
        Check((await svc.GetCancellationAsync(leave.Id,"admin@test.local"))?.Status=="Pending" &&
            await svc.GetCancellationAsync(int.MaxValue,"admin@test.local")==null, "Cancellation details resolve only existing requests");
        var denied=false;try{await svc.GetCancellationsAsync("employee@test.local");}catch(UnauthorizedAccessException){denied=true;}
        Check(denied,"Employees cannot read the cancellation approver list");
        var invalid=false;try{await svc.GetCancellationsAsync("admin@test.local",new(){From=start.AddDays(1),To=start});}catch(InvalidOperationException){invalid=true;}
        Check(invalid,"Cancellation filter rejects inverted date ranges");
    });
    await Task.WhenAll(Enumerable.Range(0,5).Select(_=>WithService(async(svc,_)=>{await svc.DecideCancellationAsync(leave.Id,true,"admin@test.local");})));
    setup.ChangeTracker.Clear();Check((await setup.EmployeePortals.FindAsync(id))!.LeaveDays==35,"Duplicate cancellation decisions refund once");
    await WithService(async(svc,_)=>{
        Check((await svc.GetCancellationsAsync("admin@test.local")).TotalCount==0,"Resolved cancellations leave the default pending list");
        var approved = await svc.GetCancellationsAsync("admin@test.local",new(){Status="Approved",Page=999});
        Check(approved.TotalCount==1 && approved.Filter.Page==1 && approved.Items[0].Status=="Approved",
            "Resolved status filter and page clamping work on MySQL");
    });
    var before=await setup.LeaveMovements.CountAsync();
    try{await WithService(async(svc,_)=>await svc.WithEmployeeAsync(id,async employee=>{var account=await svc.EnsureAccountAsync(employee);await svc.MoveAsync(employee,account,50,"Manual",Guid.NewGuid().ToString(),"admin@test.local","Rollback test",LeaveAccountingService.Today);await Task.FromException(new InvalidOperationException("injected failure"));return false;}));}catch(InvalidOperationException){}
    setup.ChangeTracker.Clear();Check(await setup.LeaveMovements.CountAsync()==before && (await setup.EmployeePortals.FindAsync(id))!.LeaveDays==35,"Injected failure rolls back movement and balance");
    await WithService(async(svc,_)=>await svc.SaveAgreementAsync(id,-3,LeaveAccountingService.Today,0,0,false,"admin@test.local"));
    setup.ChangeTracker.Clear();Check((await setup.EmployeePortals.FindAsync(id))!.LeaveDays==-3,"Negative reconciliation is supported");

    async Task<int> AddPerson(string email)
    {
        var person2=new EmployeePortal{Email=email,HireDate=LeaveAccountingService.Today.AddYears(-2).AddMonths(-2),AnnualLeaveProcessedThrough=LeaveAccountingService.Today,LeaveDays=0};
        setup.EmployeePortals.Add(person2);await setup.SaveChangesAsync();
        await WithService(async(svc,_)=>{await svc.RefreshAsync(person2.Id,LeaveAccountingService.Today);});
        return person2.Id;
    }
    var splitId=await AddPerson("split@test.local");
    var cutoff=LeaveAccountingService.Today.AddDays(-14);
    while(cutoff.DayOfWeek!=DayOfWeek.Wednesday)cutoff=cutoff.AddDays(-1);
    var splitLeave=new Leave{EmployeeId=splitId,LeaveTypeId=annual.Id,StartDate=cutoff.AddDays(-1).AddHours(9),EndDate=cutoff.AddDays(1).AddHours(18),RequestedDays=3,Status=4,Reason="Split"};
    setup.Leaves.Add(splitLeave);await setup.SaveChangesAsync();
    await WithService(async(svc,_)=>{
        await svc.CaptureCalculationAsync(splitLeave);
        await svc.ReviewHistoricalAsync(splitLeave.Id,false,"Not in opening","admin@test.local");
        await svc.SetStatusAsync(splitLeave.Id,1,"admin@test.local");
        await svc.SaveAgreementAsync(splitId,10,cutoff,0,0,false,"admin@test.local");
    });
    setup.ChangeTracker.Clear();
    Check((await setup.EmployeePortals.FindAsync(splitId))!.LeaveDays==9,"Cutoff day included: two days in opening and only one day debited");
    var lateId=await AddPerson("late@test.local");
    await WithService(async(svc,_)=>await svc.SaveAgreementAsync(lateId,10,cutoff,0,0,false,"admin@test.local"));
    var late=new Leave{EmployeeId=lateId,LeaveTypeId=annual.Id,StartDate=cutoff.AddDays(-1).AddHours(9),EndDate=cutoff.AddDays(1).AddHours(18),RequestedDays=3,Status=4,Reason="Late"};
    setup.Leaves.Add(late);await setup.SaveChangesAsync();
    await WithService(async(svc,_)=>{
        await svc.CaptureCalculationAsync(late);
        var blocked=false;try{await svc.SetStatusAsync(late.Id,1,"admin@test.local");}catch(InvalidOperationException){blocked=true;}
        Check(blocked,"Late approval requires explicit opening review");
        await svc.ReviewHistoricalAsync(late.Id,true,"Included before cutoff","admin@test.local");
        await svc.SetStatusAsync(late.Id,1,"admin@test.local");
    });
    setup.ChangeTracker.Clear();Check((await setup.EmployeePortals.FindAsync(lateId))!.LeaveDays==9,"Late approval debits only the amount missing from opening");
    Check((await setup.LeaveCharges.SingleAsync(x=>x.LeaveId==late.Id)).Days==3,"Opening deduction remains in the refundable charge without being debited twice");
    var signatureId=(await setup.LeaveAgreements.SingleAsync(x=>x.EmployeePortalId==splitId)).Id;
    await WithService(async(svc,_)=>{
        await svc.UploadAgreementPdfAsync(new MEC.Application.Abstractions.Service.LeaveService.Model.AdminLeaveAgreementPdfUpdateModel{Id=signatureId,CurrentUser="admin@test.local",FileName="original-signed.pdf",OriginalFileName="signed.pdf",SizeBytes=100});
        await svc.SaveAgreementAsync(splitId,10,cutoff,0,0,true,"admin@test.local");
        await svc.SaveAgreementAsync(splitId,-5,cutoff,0,0,true,"admin@test.local");
    });
    setup.ChangeTracker.Clear();
    var revised=await setup.LeaveAgreements.SingleAsync(x=>x.Id==signatureId);
    var archived=await setup.LeaveAgreementVersions.Where(x=>x.AgreementId==signatureId).ToListAsync();
    Check(!revised.IsSigned && revised.AgreementPdfFileName==null && archived.Any(x=>x.Snapshot.Contains("original-signed.pdf") && JsonDocument.Parse(x.Snapshot).RootElement.GetProperty("IsSigned").GetBoolean()),"Signed original is archived; changed balance is active and unsigned");
    var employmentId=await AddPerson("employment@test.local");
    await WithService(async(svc,ctx)=>{
        await svc.WithEmployeeAsync(employmentId,async employee=>{
            await svc.ValidateProfileChangeAsync(employee,employee.HireDate,employee.BirthDate,LeaveAccountingService.Today.AddDays(-7),true,"admin@test.local");employee.IsDeleted=true;return true;
        });
        await svc.SaveAgreementAsync(employmentId,2,LeaveAccountingService.Today,0,0,false,"admin@test.local",LeaveAccountingService.Today,null,"Rehired",true);
        await svc.RefreshAsync(employmentId,LeaveAccountingService.Today);
    });
    setup.ChangeTracker.Clear();var rehired=await setup.EmployeePortals.FindAsync(employmentId);
    Check(rehired!.LeaveDays==2 && !rehired.IsDeleted && rehired.HireDate==LeaveAccountingService.Today && rehired.TerminationDate==null,"Rehire uses new hire date and reconciliation without idle-period rights");
    await WithService(async(svc,ctx)=>{
        var beforeRevision=(await ctx.LeaveAccounts.SingleAsync(x=>x.EmployeeId==employmentId)).Revision;
        await svc.WithEmployeeAsync(employmentId,async employee=>{
            var checkpoint=employee.AnnualLeaveProcessedThrough;
            await svc.ValidateProfileChangeAsync(employee,employee.HireDate,employee.BirthDate,null,false,"admin@test.local");
            employee.PhoneNumber="05550000000";
            Check(employee.AnnualLeaveProcessedThrough==checkpoint,"Contact edits preserve the job checkpoint");return true;
        });
        Check((await ctx.LeaveAccounts.SingleAsync(x=>x.EmployeeId==employmentId)).Revision==beforeRevision,"Contact edits preserve ledger revision");
        var preview=await svc.PreviewDatesAsync(employmentId,LeaveAccountingService.Today,null,"admin@test.local");
        await svc.ManualAsync(employmentId,1,"Concurrent edit","admin@test.local",Guid.NewGuid().ToString());
        var stale=false;try{await svc.CorrectDatesAsync(employmentId,LeaveAccountingService.Today,null,preview.Token,"Stale","admin@test.local");}catch(InvalidOperationException){stale=true;}
        Check(stale,"Stale date preview is rejected");
        Check(await svc.CalendarErrorAsync(LeaveAccountingService.Today.AddYears(5),LeaveAccountingService.Today.AddYears(5).AddDays(1))!=null,"Missing future calendar blocks requests");
        var denied=false;try{await svc.ImportRowAsync(employmentId,99,"Forbidden","employee@test.local","unauthorized","employment@test.local");}catch(UnauthorizedAccessException){denied=true;}
        Check(denied,"Excel service rejects a non-admin actor");
    });

    var policyId=await AddPerson("policy@test.local");
    var nextFriday=LeaveAccountingService.Today.AddDays(10);
    while(nextFriday.DayOfWeek!=DayOfWeek.Friday)nextFriday=nextFriday.AddDays(1);
    var policyLeave=new Leave{EmployeeId=policyId,LeaveTypeId=annual.Id,StartDate=nextFriday.AddHours(9),EndDate=nextFriday.AddDays(3).AddHours(18),RequestedDays=2,Status=4,Reason="Policy"};
    setup.Leaves.Add(policyLeave);await setup.SaveChangesAsync();
    await WithService(async(svc,_)=>{await svc.SetStatusAsync(policyLeave.Id,1,"admin@test.local");});
    await WithService(async(svc,ctx)=>{
        var calendarService=new LeaveCalendarService(new HttpClient(),ctx,svc);
        var preview=await calendarService.PreviewAsync(null,LeaveAccountingService.Today.AddYears(-1),true,"admin@test.local");
        Check(preview.Changes.Any(x=>x.LeaveId==policyLeave.Id && x.Before==2 && x.After==3) &&
              preview.Changes.All(x=>x.LeaveId!=splitLeave.Id),"Saturday preview updates future leave and excludes already-started leave");
        await calendarService.PublishAsync(null,LeaveAccountingService.Today.AddYears(-1),true,preview.Token,"admin@test.local");
        Check((await ctx.EmployeePortals.AsNoTracking().SingleAsync(x=>x.Id==policyId)).LeaveDays==-3,"Policy publication updates days and negative balance together");
        var again=await calendarService.PreviewAsync(null,LeaveAccountingService.Today.AddYears(-1),false,"admin@test.local");
        await svc.ManualAsync(policyId,1,"Changes after preview","admin@test.local",Guid.NewGuid().ToString());
        var rejected=false;try{await calendarService.PublishAsync(null,LeaveAccountingService.Today.AddYears(-1),false,again.Token,"admin@test.local");}catch(InvalidOperationException){rejected=true;}
        Check(rejected,"Policy publication rejects a stale preview");
        await svc.SaveAgreementAsync(splitId,10,cutoff,0,0,false,"admin@test.local");
        Check((await ctx.EmployeePortals.AsNoTracking().SingleAsync(x=>x.Id==splitId)).LeaveDays==9,"Started leave keeps its calculation snapshot after policy changes");
        var expiredLeave=await ctx.Leaves.SingleAsync(x=>x.Id==policyLeave.Id);
        ctx.LeaveCancellations.Add(new LeaveCancellation{LeaveId=policyLeave.Id,RequestedBy="policy@test.local",Reason="Expired test"});
        expiredLeave.StartDate=LeaveAccountingService.Today.AddDays(-1).AddHours(9);await ctx.SaveChangesAsync();
        Check(!await svc.DecideCancellationAsync(policyLeave.Id,true,"admin@test.local"),"Cancellation cannot refund a leave that has started");
        Check((await ctx.LeaveCancellations.SingleAsync(x=>x.LeaveId==policyLeave.Id)).Status=="Expired","Started cancellation request is closed");
        var notified=false;
        await svc.WithEmployeeAsync(policyId,async employee=>{
            await svc.NotifyAsync(()=>{notified=true;throw new InvalidOperationException("Notification unavailable");});
            Check(!notified,"Notification is deferred while transaction is open");return true;
        });
        Check(notified,"Notification failure after commit does not roll back accounting");
    });

    try { await WithService(async(svc,_)=>await svc.WithEmployeeAsync(correctionPerson.Id,async employee=>{
        await svc.ValidateProfileChangeAsync(employee,employee.HireDate,employee.BirthDate,LeaveAccountingService.Today,true,"admin@test.local");
        employee.IsDeleted=true;await Task.FromException(new InvalidOperationException("Exit failure"));return true;
    })); } catch(InvalidOperationException) {}
    setup.ChangeTracker.Clear();
    var exitRollback=await setup.EmployeePortals.FindAsync(correctionPerson.Id);
    Check(!exitRollback!.IsDeleted && exitRollback.LeaveDays==14,"Failed exit rolls back both deactivation and accrual adjustment");
    await WithService(async(svc,_)=>await svc.WithEmployeeAsync(correctionPerson.Id,async employee=>{
        await svc.ValidateProfileChangeAsync(employee,employee.HireDate,employee.BirthDate,LeaveAccountingService.Today,true,"admin@test.local");
        employee.IsDeleted=true;return true;
    }));
    setup.ChangeTracker.Clear();var exited=await setup.EmployeePortals.FindAsync(correctionPerson.Id);
    Check(exited!.IsDeleted && exited.LeaveDays==0 && exited.TerminationDate==LeaveAccountingService.Today,"Exit reverses rights later than termination and deactivates atomically");
    var initialCount=await setup.LeaveMovements.CountAsync(x=>x.Kind=="Opening");
    await WithService(async(svc,_)=>{await svc.RefreshAsync(employmentId,LeaveAccountingService.Today);await svc.RefreshAsync(employmentId,LeaveAccountingService.Today);});
    Check(await setup.LeaveMovements.CountAsync(x=>x.Kind=="Opening")==initialCount,"Repeated initialization does not duplicate opening movements");
    var bad=new EmployeePortal{Email="incomplete@test.local"};setup.EmployeePortals.Add(bad);await setup.SaveChangesAsync();
    await WithService(async(svc,ctx)=>{try{await new AnnualLeaveAccrualJob(ctx,svc,NullLogger<AnnualLeaveAccrualJob>.Instance).RunAsync(LeaveAccountingService.Today);}catch(InvalidOperationException){}});
    Check(await setup.LeaveJobResults.AnyAsync(x=>!x.Success && x.EmployeeId==bad.Id) && await setup.LeaveJobResults.AnyAsync(x=>x.Success && x.EmployeeId==id),"One employee failure does not stop other job results");
}
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode=1; }
finally { await setup.Database.EnsureDeletedAsync(); }

sealed class TestWorkflow : IApprovalWorkflowService
{
    public Task<ApprovalActorModel?> GetActorAsync(string email) => Task.FromResult<ApprovalActorModel?>(new() { Email=email, IsAdministrator=email=="admin@test.local", IsFinalApprover=email=="admin@test.local" });
    public Task<ApprovalRouteModel?> ResolveRouteAsync(int id)=>Task.FromResult<ApprovalRouteModel?>(new(){EmployeePortalId=id,EmployeeEmail="employee@test.local",LocationIds=[1],FinalApprover=new(){Email="admin@test.local"}});
    public async Task<Dictionary<int,ApprovalRouteModel>> ResolveRoutesAsync(IEnumerable<int> ids){var result=new Dictionary<int,ApprovalRouteModel>();foreach(var id in ids)result[id]=(await ResolveRouteAsync(id))!;return result;}
}

sealed class OfficialCalendarHandler(string calendarHtml) : HttpMessageHandler
{
    public int RequestCount { get; private set; }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.UserAgent.ToString() != "MEC-Portal/1.0")
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.Forbidden));
        RequestCount++;
        var content = request.RequestUri!.AbsolutePath == "/"
            ? "<a href=icerik.php?icerik=154>2027 Yılı Dini Günler</a>" : calendarHtml;
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(content) });
    }
}
