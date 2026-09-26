using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;
using MEC.Application.Abstractions.Service.ApprovalWorkflowService.Model;
using MEC.Application.Abstractions.Service.LeaveService.Model;
using MEC.Application.Abstractions.Service.LoggingService;
using MEC.Application.Abstractions.Service.LoggingService.Model;
using MEC.Application.Abstractions.Service.NotificationService;
using MEC.Application.Abstractions.Service.NotificationService.Model;
using MEC.Application.Service.ApprovalWorkflowService;
using MEC.Application.Service.SchoolService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Common;
using MEC.Domain.Common.Enum;
using MEC.Domain.Entity.Employee;
using MEC.Domain.Entity.Leave;
using MEC.Domain.Entity.Overtime;
using MEC.Domain.Entity.School;

var kosuyolu = new Location { Id = 1, Name = "Koşuyolu" };
var bahcekoy = new Location { Id = 2, Name = "Bahçeköy" };

var employee = new EmployeePortal
{
    Id = 1,
    FirstName = "Test",
    LastName = "Çalışan",
    Email = "test.employee@mec.local",
    HireDate = new DateTime(2020, 4, 15),
    LeaveDays = 20
};
var manager = new EmployeePortal
{
    Id = 2,
    FirstName = "Koşuyolu",
    LastName = "Müdürü",
    Email = "test.manager.kosuyolu@mec.local",
    IsManager = true
};
var otherManager = new EmployeePortal
{
    Id = 3,
    FirstName = "Bahçeköy",
    LastName = "Müdürü",
    Email = "test.manager.bahcekoy@mec.local",
    IsManager = true
};
var finalApprover = new EmployeePortal
{
    Id = 4,
    FirstName = "Mustafa",
    LastName = "Meral",
    Email = "mustafa.meral@mecokullari.k12.tr"
};

kosuyolu.ManagerEmployeePortalId = manager.Id;
kosuyolu.Manager = manager;
bahcekoy.ManagerEmployeePortalId = otherManager.Id;
bahcekoy.Manager = otherManager;
var locations = new MemoryRepository<Location>(kosuyolu, bahcekoy);

var employees = new MemoryRepository<EmployeePortal>(employee, manager, otherManager, finalApprover);
var assignments = new MemoryRepository<EmployeePortalLocation>(
    Link(1, employee, kosuyolu),
    Link(2, manager, kosuyolu),
    Link(3, otherManager, bahcekoy));
var workflow = new ApprovalWorkflowService(
    employees,
    assignments,
    new ApprovalWorkflowSettings(), locations);

var bulkRoutes = await workflow.ResolveRoutesAsync(new[] { employee.Id, manager.Id, 999 });
Check(!bulkRoutes[employee.Id].EmployeeIsLocationManager && bulkRoutes[manager.Id].EmployeeIsLocationManager,
    "Bir okulda çalışmak müdürlük yetkisi vermemeli; okul müdürü ataması esas alınmalı.");
Check(bulkRoutes.Count == 2 &&
      bulkRoutes[employee.Id].ManagerApprovers.Single().Email == manager.Email &&
      bulkRoutes[manager.Id].ManagerApprovers.Count == 0,
    "Toplu onay rotası çözümü tekil çözümle aynı kapsamı üretmeli.");
Check(employees.GetAllCallCount == 2 && assignments.GetAllCallCount == 1 && locations.GetAllCallCount == 1,
    "Toplu onay rotası çözümü çalışan sayısıyla artan sorgular üretmemeli.");

var libraryRoot = new LibraryFolder { Id = 100, Name = "Kök", DisplayOrder = 1 };
var libraryChild = new LibraryFolder { Id = 101, Name = "Alt", ParentFolderId = libraryRoot.Id, DisplayOrder = 1 };
var libraryGrandchild = new LibraryFolder { Id = 102, Name = "Torun", ParentFolderId = libraryChild.Id, DisplayOrder = 1 };
var libraryTarget = new LibraryFolder { Id = 103, Name = "Hedef", DisplayOrder = 2 };
var libraryDocument = new LibraryDocument { Id = 200, FolderId = libraryRoot.Id, OriginalFileName = "test.pdf" };
var libraryFolders = new MemoryRepository<LibraryFolder>(libraryRoot, libraryChild, libraryGrandchild, libraryTarget);
var libraryDocuments = new MemoryRepository<LibraryDocument>(libraryDocument);
var libraryService = new LibraryService(libraryFolders, libraryDocuments);

var moveDocumentResult = await libraryService.MoveDocumentAsync(libraryDocument.Id, libraryTarget.Id);
Check(moveDocumentResult.IsSuccess && libraryDocument.FolderId == libraryTarget.Id,
    "Doküman sürükle-bırak hedef klasörüne taşınabilmeli.");

var cyclicFolderMoveResult = await libraryService.MoveFolderAsync(libraryRoot.Id, libraryGrandchild.Id);
Check(!cyclicFolderMoveResult.IsSuccess && libraryRoot.ParentFolderId == null,
    "Klasör kendi alt klasörlerinden birinin içine taşınamamalı.");

var moveFolderResult = await libraryService.MoveFolderAsync(libraryChild.Id, libraryTarget.Id);
Check(moveFolderResult.IsSuccess && libraryChild.ParentFolderId == libraryTarget.Id,
    "Klasör sürükle-bırak hedef klasörünün içine taşınabilmeli.");

var annualType = new LeaveType
{
    Id = 1,
    Name = "Yıllık İzin",
    Code = LeaveTypeCodes.Annual,
    IsActive = true
};
var marriageType = new LeaveType
{
    Id = 8,
    Name = "Evlilik İzni",
    Code = LeaveTypeCodes.Marriage,
    IsActive = true
};
var leaveTypes = new MemoryRepository<LeaveType>(annualType, marriageType);
var leaves = new MemoryRepository<Leave>();
var agreements = new MemoryRepository<LeaveAgreement>();
var policySettings = new MemoryRepository<LeavePolicySetting>();
var notifications = new CaptureNotifications();
var logs = new CaptureLogs();
var leaveService = new LeaveService(
    leaves,
    employees,
    leaveTypes,
    new MemoryRepository<Holiday>(),
    agreements,
    policySettings,
    assignments,
    new MemoryRepository<Location>(kosuyolu, bahcekoy),
    null!,
    logs,
    notifications,
    workflow);

var fiveDayWithoutApproval = await leaveService.ValidateLeaveRequestAsync(new LeaveRequestCreateModel
{
    UserEmail = employee.Email,
    LeaveTypeId = annualType.Id,
    StartDate = new DateTime(2026, 8, 17, 9, 0, 0),
    EndDate = new DateTime(2026, 8, 21, 18, 0, 0),
    Reason = "Beş günlük blok testi"
});
Check(fiveDayWithoutApproval.IsSuccess, "5 günlük yıllık izin ek onay kutusu olmadan kabul edilmeli.");

var fiveDayWithApproval = await leaveService.ValidateLeaveRequestAsync(new LeaveRequestCreateModel
{
    UserEmail = employee.Email,
    LeaveTypeId = annualType.Id,
    StartDate = new DateTime(2026, 8, 17, 9, 0, 0),
    EndDate = new DateTime(2026, 8, 21, 18, 0, 0),
    MinimumBlockExceptionRequested = true,
    Reason = "Beş günlük karşılıklı onay testi"
});
Check(fiveDayWithApproval.IsSuccess, "5 günlük yıllık izin karşılıklı onay talebiyle kabul edilmeli.");

var halfDayValidation = await leaveService.ValidateLeaveRequestAsync(new LeaveRequestCreateModel
{
    UserEmail = employee.Email,
    LeaveTypeId = annualType.Id,
    StartDate = new DateTime(2026, 8, 24, 9, 0, 0),
    EndDate = new DateTime(2026, 8, 24, 13, 0, 0),
    Reason = "Özel durum yarım gün testi"
});
Check(halfDayValidation.IsSuccess && halfDayValidation.RequestedDays == 0.5m,
    "Özel durumda yarım gün yıllık izin kullanılabilmeli.");

var excessiveMarriageLeave = await leaveService.ValidateLeaveRequestAsync(new LeaveRequestCreateModel
{
    UserEmail = employee.Email,
    LeaveTypeId = marriageType.Id,
    StartDate = new DateTime(2026, 8, 17, 9, 0, 0),
    EndDate = new DateTime(2026, 8, 20, 18, 0, 0),
    Reason = "Evlilik izni süre sınırı testi"
});
Check(!excessiveMarriageLeave.IsSuccess,
    "Evlilik izni 3 günlük sınırı aşmamalı.");

var createResult = await leaveService.CreateLeaveRequestAsync(new LeaveRequestCreateModel
{
    UserEmail = employee.Email,
    LeaveTypeId = annualType.Id,
    StartDate = new DateTime(2026, 8, 17, 9, 0, 0),
    EndDate = new DateTime(2026, 8, 28, 18, 0, 0),
    Reason = "İki aşamalı akış testi"
});
Check(createResult.IsSuccess, "Çalışan izin talebi oluşturabilmeli.");

var leave = await leaves.GetByIdAsync(createResult.Data!.LeaveId);
leave.LeaveType = annualType;
Check(leave.Status == (int)LeaveStatus.Pending, "Yeni talep okul müdürü onayı beklemeli.");

await leaveService.DispatchLeaveRequestCreatedNotificationsAsync(new LeaveRequestCreatedDispatchModel
{
    LeaveId = leave.Id,
    UserEmail = employee.Email,
    IpAddress = "127.0.0.1"
});
var createdMail = notifications.LeaveCreated.Single();
Check(createdMail.Approvers.Count == 1 && createdMail.Approvers[0].Email == manager.Email,
    "İlk onay e-postası yalnızca ilgili lokasyon müdürüne gitmeli.");

var earlyFinalDecision = await leaveService.UpdateLeaveStatusWithLogAsync(new LeaveStatusUpdateRequestModel
{
    LeaveId = leave.Id,
    Status = (int)LeaveStatus.Approved,
    CurrentUser = finalApprover.Email,
    DecisionBy = "Mustafa Meral"
});
Check(!earlyFinalDecision.IsSuccess && leave.Status == (int)LeaveStatus.Pending,
    "Nihai onaylayıcı müdür aşamasını atlayamamalı.");

var unrelatedManagerDecision = await leaveService.UpdateLeaveStatusWithLogAsync(new LeaveStatusUpdateRequestModel
{
    LeaveId = leave.Id,
    Status = (int)LeaveStatus.Approved,
    CurrentUser = otherManager.Email,
    DecisionBy = "Bahçeköy Müdürü"
});
Check(!unrelatedManagerDecision.IsSuccess && leave.Status == (int)LeaveStatus.Pending,
    "Başka lokasyonun müdürü talebi onaylayamamalı.");

var managerDecision = await leaveService.UpdateLeaveStatusWithLogAsync(new LeaveStatusUpdateRequestModel
{
    LeaveId = leave.Id,
    Status = (int)LeaveStatus.Approved,
    CurrentUser = manager.Email,
    DecisionBy = "Koşuyolu Müdürü"
});
Check(managerDecision.IsSuccess, "İlgili okul müdürü talebi onaylayabilmeli.");
Check(leave.Status == (int)LeaveStatus.PendingFinalApproval,
    "Müdür onayından sonra talep Genel Müdürlük onayına geçmeli.");
Check(employee.LeaveDays == 20, "Müdür onayında yıllık izin bakiyesi düşmemeli.");
var managerMail = notifications.LeaveDecisions.Single();
Check(managerMail.IsManagerDecision && managerMail.NextApprovers.Single().Email == finalApprover.Email,
    "Müdür onayından sonra çalışan ve Mustafa Meral bildirim modeli oluşmalı.");

var finalDecision = await leaveService.UpdateLeaveStatusWithLogAsync(new LeaveStatusUpdateRequestModel
{
    LeaveId = leave.Id,
    Status = (int)LeaveStatus.Approved,
    CurrentUser = finalApprover.Email,
    DecisionBy = "Mustafa Meral"
});
Check(finalDecision.IsSuccess && leave.Status == (int)LeaveStatus.Approved,
    "Mustafa Meral nihai onayı verebilmeli.");
Check(employee.LeaveDays == 20 - leave.RequestedDays,
    "Yıllık izin bakiyesi yalnızca nihai onaydan sonra düşmeli.");
var finalMail = notifications.LeaveDecisions.Last();
Check(!finalMail.IsManagerDecision && finalMail.RegionalManagers.Single().Email == manager.Email,
    "Nihai karar çalışan ve okul müdürü için bildirim modeli oluşturmalı.");

var balanceAfterApproval = employee.LeaveDays;
var managerRejectLeave = new Leave
{
    Id = 20,
    EmployeeId = employee.Id,
    LeaveTypeId = annualType.Id,
    LeaveType = annualType,
    StartDate = new DateTime(2026, 8, 18, 9, 0, 0),
    EndDate = new DateTime(2026, 8, 18, 18, 0, 0),
    RequestedDays = 1,
    Reason = "Müdür ret testi",
    Status = (int)LeaveStatus.Pending
};
leaves.Items.Add(managerRejectLeave);
var managerRejectResult = await leaveService.UpdateLeaveStatusWithLogAsync(new LeaveStatusUpdateRequestModel
{
    LeaveId = managerRejectLeave.Id,
    Status = (int)LeaveStatus.Rejected,
    CurrentUser = manager.Email,
    DecisionBy = "Koşuyolu Müdürü"
});
Check(managerRejectResult.IsSuccess && managerRejectLeave.Status == (int)LeaveStatus.Rejected,
    "Okul müdürü talebi reddedebilmeli.");
Check(employee.LeaveDays == balanceAfterApproval,
    "Okul müdürü reddinde izin bakiyesi değişmemeli.");

var finalRejectLeave = new Leave
{
    Id = 21,
    EmployeeId = employee.Id,
    LeaveTypeId = annualType.Id,
    LeaveType = annualType,
    StartDate = new DateTime(2026, 8, 19, 9, 0, 0),
    EndDate = new DateTime(2026, 8, 19, 18, 0, 0),
    RequestedDays = 1,
    Reason = "Nihai ret testi",
    Status = (int)LeaveStatus.Pending
};
leaves.Items.Add(finalRejectLeave);
var managerApprovalBeforeReject = await leaveService.UpdateLeaveStatusWithLogAsync(new LeaveStatusUpdateRequestModel
{
    LeaveId = finalRejectLeave.Id,
    Status = (int)LeaveStatus.Approved,
    CurrentUser = manager.Email,
    DecisionBy = "Koşuyolu Müdürü"
});
var finalRejectResult = await leaveService.UpdateLeaveStatusWithLogAsync(new LeaveStatusUpdateRequestModel
{
    LeaveId = finalRejectLeave.Id,
    Status = (int)LeaveStatus.Rejected,
    CurrentUser = finalApprover.Email,
    DecisionBy = "Mustafa Meral"
});
Check(managerApprovalBeforeReject.IsSuccess && finalRejectResult.IsSuccess && finalRejectLeave.Status == (int)LeaveStatus.Rejected,
    "Mustafa Meral müdürün onayladığı talebi reddedebilmeli.");
Check(employee.LeaveDays == balanceAfterApproval,
    "Nihai ret kararında izin bakiyesi değişmemeli.");

assignments.Items.Add(Link(4, employee, bahcekoy));
var multiLocationRoute = await workflow.ResolveRouteAsync(employee.Id);
Check(multiLocationRoute != null && multiLocationRoute.LocationIds.Count == 2,
    "Bir çalışana birden fazla lokasyon atanabilmeli.");
Check(multiLocationRoute!.ManagerApprovers.Select(x => x.Email).ToHashSet().SetEquals(new[] { manager.Email, otherManager.Email }),
    "Çoklu lokasyon çalışanında ilgili tüm lokasyon müdürleri onay rotasına girmeli.");

var agreement = new LeaveAgreement
{
    Id = 1,
    EmployeePortalId = employee.Id,
    EmployeePortal = employee
};
agreements.Items.Add(agreement);
var agreementResult = await leaveService.UpdateLeaveAgreementAsync(new AdminLeaveAgreementUpdateModel
{
    Id = agreement.Id,
    AgreedLeaveDays = 10m,
    BalanceAsOfDate = new DateTime(2026, 8, 5),
    CurrentYearEarnedDays = 14m,
    CurrentYearUsedDays = 7m,
    IsSigned = true
});
Check(agreementResult.IsSuccess && employee.LeaveDays == 0m,
    $"Mutabakat bakiyesi esas alınmalı; bilgi amaçlı 2026 alanları bakiyeye tekrar eklenip çıkarılmamalı. Bakiye: {employee.LeaveDays:0.##}");

var managerBalances = await leaveService.GetAdminLeaveBalancesAsync(new AdminLeaveBalanceQueryModel
{
    CurrentUserEmail = manager.Email,
    PageSize = 20
});
Check(managerBalances.IsAuthorized && !managerBalances.CanViewAllLocations &&
      managerBalances.Items.Any(x => x.EmployeePortalId == employee.Id) &&
      managerBalances.Items.All(x => x.LocationNames.Contains(kosuyolu.Name, StringComparison.OrdinalIgnoreCase)),
    "Okul müdürü yalnızca sorumlu olduğu okulun çalışan izin bakiyelerini görebilmeli.");

var managerUnauthorizedSchoolFilter = await leaveService.GetAdminLeaveBalancesAsync(new AdminLeaveBalanceQueryModel
{
    CurrentUserEmail = manager.Email,
    LocationId = bahcekoy.Id,
    PageSize = 20
});
Check(managerUnauthorizedSchoolFilter.TotalCount == 0,
    "Okul müdürü sorgu parametresini değiştirerek sorumlu olmadığı okulu görüntüleyememeli.");

var finalApproverBalances = await leaveService.GetAdminLeaveBalancesAsync(new AdminLeaveBalanceQueryModel
{
    CurrentUserEmail = finalApprover.Email,
    SearchText = "Müdürü",
    PageSize = 20
});
Check(finalApproverBalances.IsAuthorized && finalApproverBalances.CanViewAllLocations &&
      finalApproverBalances.TotalCount == 2 &&
      finalApproverBalances.Items.Select(x => x.EmployeePortalId).ToHashSet().SetEquals(new[] { manager.Id, otherManager.Id }),
    "Mustafa Meral tüm okullarda arama yaparak çalışan izin bakiyelerini görebilmeli.");

Check(AnnualLeaveEntitlementCalculator.CalculateEntitlement(
        new DateTime(2025, 8, 21), null, new DateTime(2026, 8, 21)) == 14m,
    "İlk yıl tamamlandığında 14 gün hak edilmelidir.");
Check(AnnualLeaveEntitlementCalculator.CalculateEntitlement(
        new DateTime(2025, 8, 22), null, new DateTime(2026, 8, 21)) == 0m,
    "İlk çalışma yılı tamamlanmadan yıllık izin hak edilmemelidir.");
Check(AnnualLeaveEntitlementCalculator.CalculateEntitlement(
        new DateTime(2020, 8, 21), null, new DateTime(2026, 8, 21)) == 20m,
    "Beş yıldan fazla kıdemde 20 gün hak edilmelidir.");
Check(AnnualLeaveEntitlementCalculator.CalculateEntitlement(
        new DateTime(2011, 8, 21), null, new DateTime(2026, 8, 21)) == 26m,
    "On beş yıl kıdemde 26 gün hak edilmelidir.");
Check(AnnualLeaveEntitlementCalculator.CalculateEntitlement(
        new DateTime(2025, 8, 21), new DateTime(1976, 8, 21), new DateTime(2026, 8, 21)) == 20m,
    "50 yaşındaki çalışan en az 20 gün hak etmelidir.");
Check(AnnualLeaveEntitlementCalculator.CalculateEntitlement(
        new DateTime(2025, 8, 21), new DateTime(2008, 8, 21), new DateTime(2026, 8, 21)) == 20m,
    "18 yaşındaki çalışan en az 20 gün hak etmelidir.");

var saturdayWeek = LeaveDurationCalculator.CalculateRequestedDays(
    new DateTime(2026, 8, 17, 9, 0, 0),
    new DateTime(2026, 8, 22, 18, 0, 0),
    saturdayPolicies: new[] { new SaturdayLeavePolicy(new DateTime(2026, 1, 1), true) });
Check(saturdayWeek == 6m, "Cumartesi parametresi açıkken pazartesi-cumartesi 6 gün sayılmalıdır.");

var halfHolidayDay = LeaveDurationCalculator.CalculateRequestedDays(
    new DateTime(2026, 10, 28, 9, 0, 0),
    new DateTime(2026, 10, 28, 18, 0, 0),
    new[] { new HolidayInterval(new DateTime(2026, 10, 28, 13, 0, 0), new DateTime(2026, 10, 29, 0, 0, 0)) });
Check(halfHolidayDay == 0.5m, "Yarım gün resmî tatilde tam gün izin talebinden 0,5 gün düşülmelidir.");

Console.WriteLine("PASS: Talep oluşturma ve okul müdürüne yönlendirme");
Console.WriteLine("PASS: Başka lokasyon müdürünün yetkisiz olması");
Console.WriteLine("PASS: Okul müdürü onayı ve Mustafa Meral'e aktarım");
Console.WriteLine("PASS: Nihai onay ve izin bakiyesi güncellemesi");
Console.WriteLine("PASS: Çalışan ve müdür bildirim alıcıları");
Console.WriteLine("PASS: Okul müdürü ret akışı ve bakiye koruması");
Console.WriteLine("PASS: Mustafa Meral ret akışı ve bakiye koruması");
Console.WriteLine("PASS: Bir kullanıcıya birden fazla lokasyon atanması");
Console.WriteLine("PASS: Kıdem ve yaşa bağlı yıllık izin hak edişi");
Console.WriteLine("PASS: Cumartesi parametresi ve yarım gün tatil hesabı");
Console.WriteLine("PASS: Blok sınırı olmadan yıllık izin talebi");
Console.WriteLine("PASS: Mutabakat açılış bakiyesi ve 2026 bilgi alanlarının ayrılması");
Console.WriteLine("PASS: Mazeret izni süre sınırları");
Console.WriteLine("PASS: Okul müdürünün izin bakiyesi lokasyon kapsamı");
Console.WriteLine("PASS: Mustafa Meral'in tüm okullarda izin bakiyesi araması");
// New regression scenarios: independent of the fixed historical workflow fixtures.
var anniversaryEmployee = new EmployeePortal { HireDate = new DateTime(2025, 9, 27), LeaveDays = -2m };
Check(AnnualLeaveAccrualCalculator.PendingDays(anniversaryEmployee, new DateTime(2026, 9, 26)) == 0m,
    "Yıl dönümünden önce izin eklenmemeli.");
Check(AnnualLeaveAccrualCalculator.PendingDays(anniversaryEmployee, new DateTime(2026, 9, 27)) == 14m,
    "Mutabakatı olmayan personelin ilk yıl dönümünde 14 gün eklenmeli.");
anniversaryEmployee.AnnualLeaveProcessedThrough = new DateTime(2026, 9, 27);
Check(AnnualLeaveAccrualCalculator.PendingDays(anniversaryEmployee, new DateTime(2026, 9, 27)) == 0m,
    "Aynı gün tekrarlanan job tekrar hak ediş üretmemeli.");
anniversaryEmployee.AnnualLeaveProcessedThrough = new DateTime(2026, 9, 26);
Check(AnnualLeaveAccrualCalculator.PendingDays(anniversaryEmployee, new DateTime(2026, 9, 29)) == 14m,
    "Kaçırılan günün hak edişi sonraki çalıştırmada yakalanmalı.");
anniversaryEmployee.TerminationDate = new DateTime(2026, 9, 26);
Check(AnnualLeaveAccrualCalculator.PendingDays(anniversaryEmployee, new DateTime(2026, 9, 29)) == 0m,
    "İşten çıktıktan sonra hak ediş oluşmamalı.");
var leapEmployee = new EmployeePortal { HireDate = new DateTime(2024, 2, 29) };
Check(AnnualLeaveAccrualCalculator.PendingDays(leapEmployee, new DateTime(2025, 2, 28)) == 14m,
    "29 Şubat işe giriş yıl dönümü 28 Şubat'ta işlenmeli.");
var veteran = new EmployeePortal { HireDate = new DateTime(2000, 1, 1) };
Check(AnnualLeaveAccrualCalculator.PendingDays(veteran, new DateTime(2026, 9, 27)) == 0m,
    "İlk job geçmiş yılların haklarını tekrar eklememeli.");
Check(AnnualLeaveEntitlementCalculator.CalculateEntitlement(new DateTime(2021, 9, 27), null, new DateTime(2026, 9, 27)) == 14m,
    "Tam beş yılda 14 gün, altıncı yıl dönümünde 20 gün olmalı.");

var zeroEmployee = new EmployeePortal { Id = 90, FirstName = "Sıfır", LastName = "Bakiye", Email = "zero@mec.local", HireDate = DateTime.Today };
employees.Items.Add(zeroEmployee);
assignments.Items.Add(Link(90, zeroEmployee, kosuyolu));
foreach (var type in new[] { annualType, marriageType })
{
    var validation = await leaveService.ValidateLeaveRequestAsync(new LeaveRequestCreateModel
    {
        UserEmail = zeroEmployee.Email, LeaveTypeId = type.Id,
        StartDate = new DateTime(2026, 10, 5, 9, 0, 0), EndDate = new DateTime(2026, 10, 5, 18, 0, 0),
        Reason = "Sıfır bakiye ve ilk yıl öncesi talep"
    });
    Check(validation.IsSuccess, "Sıfır bakiye hiçbir izin türünü engellememeli: " + type.Name);
}
var administrator = new EmployeePortal { Id = 91, Email = "assigned-admin@mec.local", IsAdmin = true };
employees.Items.Add(administrator);
var negativeLeave = new Leave { Id = 91, EmployeeId = zeroEmployee.Id, LeaveTypeId = annualType.Id,
    LeaveType = annualType, StartDate = new DateTime(2026, 10, 5), EndDate = new DateTime(2026, 10, 6),
    RequestedDays = 1, Status = (int)LeaveStatus.Pending };
leaves.Items.Add(negativeLeave);
for (var stage = 0; stage < 2; stage++)
{
    var approved = await leaveService.UpdateLeaveStatusWithLogAsync(new LeaveStatusUpdateRequestModel
    { LeaveId = negativeLeave.Id, Status = (int)LeaveStatus.Approved, CurrentUser = administrator.Email, DecisionBy = "Atanmış admin" });
    Check(approved.IsSuccess, "Admin yetkisi verilen hesap her iki onay aşamasını çalıştırabilmeli.");
}
Check(zeroEmployee.LeaveDays == -1m, "Nihai onay sıfır bakiyeyi eksi bire düşürmeli.");
await leaveService.UpdateLeaveStatusAsync(negativeLeave.Id, (int)LeaveStatus.Approved);
Check(zeroEmployee.LeaveDays == -1m, "Aynı onayın tekrarı bakiyeyi tekrar düşürmemeli.");
await leaveService.UpdateLeaveStatusAsync(negativeLeave.Id, (int)LeaveStatus.Cancelled);
Check(zeroEmployee.LeaveDays == 0m, "İptal edilen yıllık izin bakiyeye geri eklenmeli.");
var paidLeave = new Leave { Id = 92, EmployeeId = zeroEmployee.Id, LeaveTypeId = marriageType.Id,
    LeaveType = marriageType, RequestedDays = 1, Status = (int)LeaveStatus.Pending };
leaves.Items.Add(paidLeave);
await leaveService.UpdateLeaveStatusAsync(paidLeave.Id, (int)LeaveStatus.Approved);
Check(zeroEmployee.LeaveDays == 0m, "Evlilik izni yıllık izin bakiyesini azaltmamalı.");

kosuyolu.ManagerEmployeePortalId = otherManager.Id;
kosuyolu.Manager = otherManager;
var oldActor = await workflow.GetActorAsync(manager.Email);
var newActor = await workflow.GetActorAsync(otherManager.Email);
var reassignedRoute = await workflow.ResolveRouteAsync(zeroEmployee.Id);
Check(oldActor?.IsLocationManager == false && newActor!.LocationIds.Contains(kosuyolu.Id),
    "Müdür yetkisi çalıştığı lokasyondan değil okul atamasından türemeli.");
Check(reassignedRoute!.ManagerApprovers.Single().Email == otherManager.Email,
    "Müdür değişince yeni talepler yeni müdüre yönlenmeli.");
administrator.IsAdmin = false;
Check(!(await workflow.GetActorAsync(administrator.Email))!.IsAdministrator,
    "Kaldırılan admin rolü güncel kayıttan okunmalı.");
Console.WriteLine("PASS: Job yıl dönümü, tekrar, kaçırılan gün, 29 Şubat ve işten çıkış");
Console.WriteLine("PASS: Sıfır bakiye, negatif bakiye, iptal ve yıllık olmayan izin");
Console.WriteLine("PASS: Atanan admin yetkisi ve okul müdürü değişikliği");
var loginConfiguration = new TestConfiguration();
var loginService = new MEC.Application.Service.LoginService.LoginService(loginConfiguration,
    new MemoryRepository<Employee>(), employees);
Check(await loginService.ValidatePortalUserAsync(" " + zeroEmployee.Email + " ", "test-only-password"),
    "Aktif portal kullanıcısı eski employee admin kaydı olmadan oturum açabilmeli.");
zeroEmployee.IsDeleted = true;
Check(!await loginService.ValidatePortalUserAsync(zeroEmployee.Email, "test-only-password"),
    "Pasif portal kullanıcısı oturum açamamalı.");
Console.WriteLine("PASS: Portal girişi employee_portal kaydını kullanır, pasif kullanıcıyı reddeder");
var locationEditUser = new EmployeePortal { Id = 1001, Email = "location.test@example.invalid", IsAdmin = true, LeaveDays = 17 };
var locationEditAssignments = new MemoryRepository<EmployeePortalLocation>();
var locationEditService = new MEC.Application.Service.EmployeeService.EmployeePortalService(
    new MemoryRepository<EmployeePortal>(locationEditUser), new MemoryRepository<EmployeePortalChild>(),
    locationEditAssignments, locations, new MemoryRepository<Leave>(), new MemoryRepository<MEC.Domain.Entity.Loan.Loan>());
var locationSave = await locationEditService.UpdatePortalUserLocationsAsync(locationEditUser.Id, new[] { kosuyolu.Id, bahcekoy.Id, kosuyolu.Id });
Check(locationSave.IsSuccess && locationEditAssignments.Items.Count == 2,
    "Eksik profil alanları lokasyon kaydını engellememeli; tekrar eden seçimler tek kaydedilmeli.");
Check(locationEditUser.IsAdmin && locationEditUser.LeaveDays == 17,
    "Lokasyon kaydı yetki veya izin bakiyesini değiştirmemeli.");
locationSave = await locationEditService.UpdatePortalUserLocationsAsync(locationEditUser.Id, new[] { 999999 });
Check(!locationSave.IsSuccess && locationEditAssignments.Items.Count == 2,
    "Geçersiz lokasyon seçimi mevcut atamaları silmemeli.");
await locationEditService.UpdatePortalUserLocationsAsync(locationEditUser.Id, new[] { bahcekoy.Id });
Check(locationEditAssignments.Items.Single().LocationId == bahcekoy.Id, "Lokasyon seçimi güncellenebilmeli.");
await locationEditService.UpdatePortalUserLocationsAsync(locationEditUser.Id, Array.Empty<int>());
Check(locationEditAssignments.Items.Count == 0, "Tüm lokasyon seçimleri kaldırılabilmeli.");
Console.WriteLine("PASS: Lokasyonlar eksik profilden bağımsız eklenir, değiştirilir ve kaldırılır");
Console.WriteLine("SMOKE TEST RESULT: Tüm kontroller başarılı");


static EmployeePortalLocation Link(int id, EmployeePortal employee, Location location)
{
    return new EmployeePortalLocation
    {
        Id = id,
        EmployeePortalId = employee.Id,
        EmployeePortal = employee,
        LocationId = location.Id,
        Location = location
    };
}

static void Check(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException("FAIL: " + message);
    }
}

sealed class MemoryRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    public MemoryRepository(params T[] items)
    {
        Items = items.ToList();
    }

    public List<T> Items { get; }
    public int GetAllCallCount { get; private set; }

    public Task<T> GetByIdAsync(int id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id)!);

    public Task<IEnumerable<T>> GetAllAsync()
    {
        GetAllCallCount++;
        return Task.FromResult<IEnumerable<T>>(Items.ToList());
    }

    public Task<IEnumerable<T>> GetAllAsync(
        Expression<Func<T, bool>>? predicate = null,
        params Expression<Func<T, object>>[] includes)
    {
        GetAllCallCount++;
        var query = predicate == null ? Items : Items.Where(predicate.Compile());
        return Task.FromResult<IEnumerable<T>>(query.ToList());
    }

    public Task AddAsync(T entity)
    {
        if (entity.Id <= 0)
        {
            entity.Id = Items.Count == 0 ? 1 : Items.Max(x => x.Id) + 1;
        }
        Items.Add(entity);
        return Task.CompletedTask;
    }

    public void Update(T entity) { }
    public void Delete(T entity) => Items.Remove(entity);
}

sealed class CaptureLogs : IUserActionLogService
{
    public List<UserActionLogEntryModel> Entries { get; } = new();
    public Task LogAsync(UserActionLogEntryModel model)
    {
        Entries.Add(model);
        return Task.CompletedTask;
    }
}

sealed class CaptureNotifications : IWorkflowNotificationService
{
    public List<LeaveRequestCreatedNotificationModel> LeaveCreated { get; } = new();
    public List<LeaveRequestDecisionNotificationModel> LeaveDecisions { get; } = new();

    public Task NotifyLeaveRequestCreatedAsync(LeaveRequestCreatedNotificationModel model)
    {
        LeaveCreated.Add(model);
        return Task.CompletedTask;
    }

    public Task NotifyLeaveRequestDecisionAsync(LeaveRequestDecisionNotificationModel model)
    {
        LeaveDecisions.Add(model);
        return Task.CompletedTask;
    }

    public Task NotifyLeaveRequestCancelledAsync(LeaveRequestCancelledNotificationModel model) => Task.CompletedTask;
    public Task NotifyOvertimeRequestCreatedAsync(OvertimeRequestCreatedNotificationModel model) => Task.CompletedTask;
    public Task NotifyOvertimeRequestCancelledAsync(OvertimeRequestCancelledNotificationModel model) => Task.CompletedTask;
    public Task NotifyOvertimeRequestDecisionAsync(OvertimeRequestDecisionNotificationModel model) => Task.CompletedTask;
}

sealed class TestConfiguration : IConfiguration
{
    public string? this[string key] { get => key == "AppSettings:AdminPassword" ? "test-only-password" : null; set => throw new NotSupportedException(); }
    public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();
    public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => throw new NotSupportedException();
    public IConfigurationSection GetSection(string key) => throw new NotSupportedException();
}
