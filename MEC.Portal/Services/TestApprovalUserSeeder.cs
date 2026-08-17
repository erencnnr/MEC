using MEC.DAL.Config.Contexts;
using MEC.Domain.Common.Enum;
using MEC.Domain.Entity.Employee;
using Microsoft.EntityFrameworkCore;

namespace MEC.Portal.Services;

internal static class TestApprovalUserSeeder
{
    internal const string TeacherEmail = "ogretmen.test@example.invalid";
    internal const string ManagerEmail = "mudur.test@example.invalid";
    internal const string FinalApproverEmail = "mustafa.meral.test@example.invalid";

    private const string TestEnvironment = "Test";
    private const string TestLocationName = "Dummy Test Okulu";

    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger)
    {
        var environment = configuration["AppSettings:Environment"];
        var seedEnabled = configuration.GetValue<bool>("TestData:SeedApprovalUsers");

        if (!seedEnabled || !string.Equals(environment, TestEnvironment, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.Now;

        var location = await dbContext.Locations
            .SingleOrDefaultAsync(x => x.Name == TestLocationName);

        if (location == null)
        {
            location = new Location
            {
                Name = TestLocationName,
                CreatedDate = now
            };
            dbContext.Locations.Add(location);
            await dbContext.SaveChangesAsync();
        }

        var teacher = await UpsertUserAsync(
            dbContext,
            TeacherEmail,
            "Dummy",
            "Öğretmen",
            "Öğretmen",
            "05550000001",
            isManager: false,
            now);

        var manager = await UpsertUserAsync(
            dbContext,
            ManagerEmail,
            "Dummy",
            "Okul Müdürü",
            "Okul Müdürü",
            "05550000002",
            isManager: true,
            now);

        var finalApprover = await UpsertUserAsync(
            dbContext,
            FinalApproverEmail,
            "Mustafa",
            "Meral Test",
            "Genel Müdürlük Onay Yetkilisi",
            "05550000003",
            isManager: false,
            now);

        await EnsureLocationAssignmentAsync(dbContext, teacher.Id, location.Id, now);
        await EnsureLocationAssignmentAsync(dbContext, manager.Id, location.Id, now);
        await EnsureLocationAssignmentAsync(dbContext, finalApprover.Id, location.Id, now);
        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Test onay kullanıcıları hazırlandı. Öğretmen: {TeacherEmail}, Müdür: {ManagerEmail}, Son onaylayıcı: {FinalApproverEmail}",
            TeacherEmail,
            ManagerEmail,
            FinalApproverEmail);
    }

    private static async Task<EmployeePortal> UpsertUserAsync(
        ApplicationDbContext dbContext,
        string email,
        string firstName,
        string lastName,
        string title,
        string phoneNumber,
        bool isManager,
        DateTime now)
    {
        var user = await dbContext.EmployeePortals.SingleOrDefaultAsync(x => x.Email == email);
        if (user == null)
        {
            user = new EmployeePortal
            {
                Email = email,
                CreatedDate = now
            };
            dbContext.EmployeePortals.Add(user);
        }

        user.FirstName = firstName;
        user.LastName = lastName;
        user.PhoneNumber = phoneNumber;
        user.Title = title;
        user.HireDate = now.Date.AddYears(-2);
        user.TerminationDate = null;
        user.BirthDate = now.Date.AddYears(-30);
        user.AddressText = "Dummy test adresi";
        user.MaritalStatus = MaritalStatusType.Single;
        user.EducationUniversity = "Dummy Test Üniversitesi";
        user.EducationFaculty = "Test Fakültesi";
        user.EducationDepartment = "Test Bölümü";
        user.LeaveDays = 20;
        user.IsAdmin = false;
        user.IsManager = isManager;
        user.IsDeleted = false;
        user.UpdateDate = now;

        await dbContext.SaveChangesAsync();
        return user;
    }

    private static async Task EnsureLocationAssignmentAsync(
        ApplicationDbContext dbContext,
        int employeePortalId,
        int locationId,
        DateTime now)
    {
        var exists = await dbContext.EmployeePortalLocations.AnyAsync(
            x => x.EmployeePortalId == employeePortalId && x.LocationId == locationId);

        if (exists)
        {
            return;
        }

        dbContext.EmployeePortalLocations.Add(new EmployeePortalLocation
        {
            EmployeePortalId = employeePortalId,
            LocationId = locationId,
            CreatedDate = now,
            UpdateDate = now
        });
    }
}
