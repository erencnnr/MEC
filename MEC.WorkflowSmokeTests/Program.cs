using System.Linq.Expressions;
using MEC.Application.Abstractions.Service.ApprovalWorkflowService.Model;
using MEC.Application.Abstractions.Service.LeaveService.Model;
using MEC.Application.Abstractions.Service.LoggingService;
using MEC.Application.Abstractions.Service.LoggingService.Model;
using MEC.Application.Abstractions.Service.NotificationService;
using MEC.Application.Abstractions.Service.NotificationService.Model;
using MEC.Application.Service.ApprovalWorkflowService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Common;
using MEC.Domain.Common.Enum;
using MEC.Domain.Entity.Employee;
using MEC.Domain.Entity.Leave;
using MEC.Domain.Entity.Overtime;

var kosuyolu = new Location { Id = 1, Name = "Koşuyolu" };
var bahcekoy = new Location { Id = 2, Name = "Bahçeköy" };

var employee = new EmployeePortal
{
    Id = 1,
    FirstName = "Test",
    LastName = "Çalışan",
    Email = "test.employee@mec.local",
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

var employees = new MemoryRepository<EmployeePortal>(employee, manager, otherManager, finalApprover);
var assignments = new MemoryRepository<EmployeePortalLocation>(
    Link(1, employee, kosuyolu),
    Link(2, manager, kosuyolu),
    Link(3, otherManager, bahcekoy));
var workflow = new ApprovalWorkflowService(
    employees,
    assignments,
    new ApprovalWorkflowSettings());

var annualType = new LeaveType
{
    Id = 1,
    Name = "Yıllık İzin",
    Code = LeaveTypeCodes.Annual,
    IsActive = true
};
var leaves = new MemoryRepository<Leave>();
var notifications = new CaptureNotifications();
var logs = new CaptureLogs();
var leaveService = new LeaveService(
    leaves,
    employees,
    new MemoryRepository<LeaveType>(annualType),
    new MemoryRepository<Holiday>(),
    new MemoryRepository<LeaveAgreement>(),
    null!,
    logs,
    notifications,
    workflow);

var createResult = await leaveService.CreateLeaveRequestAsync(new LeaveRequestCreateModel
{
    UserEmail = employee.Email,
    LeaveTypeId = annualType.Id,
    StartDate = new DateTime(2026, 8, 17, 9, 0, 0),
    EndDate = new DateTime(2026, 8, 17, 18, 0, 0),
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

Console.WriteLine("PASS: Talep oluşturma ve okul müdürüne yönlendirme");
Console.WriteLine("PASS: Başka lokasyon müdürünün yetkisiz olması");
Console.WriteLine("PASS: Okul müdürü onayı ve Mustafa Meral'e aktarım");
Console.WriteLine("PASS: Nihai onay ve izin bakiyesi güncellemesi");
Console.WriteLine("PASS: Çalışan ve müdür bildirim alıcıları");
Console.WriteLine("PASS: Okul müdürü ret akışı ve bakiye koruması");
Console.WriteLine("PASS: Mustafa Meral ret akışı ve bakiye koruması");
Console.WriteLine("PASS: Bir kullanıcıya birden fazla lokasyon atanması");
Console.WriteLine("SMOKE TEST RESULT: 8/8 başarılı");

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

    public Task<T> GetByIdAsync(int id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id)!);

    public Task<IEnumerable<T>> GetAllAsync() => Task.FromResult<IEnumerable<T>>(Items.ToList());

    public Task<IEnumerable<T>> GetAllAsync(
        Expression<Func<T, bool>>? predicate = null,
        params Expression<Func<T, object>>[] includes)
    {
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
