using MEC.DAL.Config.Contexts;
using MEC.Domain.Common;
using MEC.Domain.Common.Enum;
using MEC.Domain.Entity.Employee;
using MEC.Domain.Entity.Leave;
using Microsoft.EntityFrameworkCore;

namespace MEC.Portal.Services;

internal static class TestApprovalUserSeeder
{
    internal const string TeacherEmail = "ogretmen.test@example.invalid";
    internal const string ManagerEmail = "mudur.test@example.invalid";
    internal const string FinalApproverEmail = "mustafa.meral.test@example.invalid";

    private const string TestEnvironment = "Test";
    private const int ExpectedUserCount = 40;

    private static readonly string[] TestSchoolNames =
    {
        "Dummy Test Okulu",
        "Dummy Test Okulu - Anadolu",
        "Dummy Test Okulu - Bilim",
        "Dummy Test Okulu - Sanat"
    };

    private const string TestHeadOfficeName = "Dummy Genel Müdürlük";

    private static readonly (string FirstName, string LastName)[] PrincipalNames =
    {
        ("Ayşe", "Demir"),
        ("Mehmet", "Kaya"),
        ("Selin", "Yıldız"),
        ("Burak", "Arslan")
    };

    private static readonly (string FirstName, string LastName)[] TeacherNames =
    {
        ("Zeynep", "Acar"),
        ("Ahmet", "Çelik"),
        ("Elif", "Şahin"),
        ("Emre", "Koç"),
        ("Derya", "Yılmaz"),
        ("Can", "Özkan"),
        ("Merve", "Güneş"),
        ("Kerem", "Aksoy"),
        ("Ece", "Kılıç"),
        ("Onur", "Aydın"),
        ("Buse", "Tekin"),
        ("Hakan", "Kurt"),
        ("İrem", "Polat"),
        ("Tolga", "Eren"),
        ("Sude", "Korkmaz"),
        ("Barış", "Yalçın"),
        ("Ceren", "Doğan"),
        ("Oğuzhan", "Karaca"),
        ("Nazlı", "Kaplan"),
        ("Serkan", "Tunç"),
        ("Melis", "Erdem"),
        ("Gökhan", "Özdemir"),
        ("Aslı", "Çetin"),
        ("Deniz", "Uysal"),
        ("Pelin", "Işık"),
        ("Kaan", "Bozkurt"),
        ("Gizem", "Keskin"),
        ("Murat", "Bulut"),
        ("Esra", "Şimşek"),
        ("Uğur", "Taş"),
        ("Damla", "Avcı"),
        ("Volkan", "Öz"),
        ("Yasemin", "Kılıç"),
        ("Berkay", "Duman"),
        ("Nihan", "Ateş")
    };

    private static readonly string[] TeacherTitles =
    {
        "Sınıf Öğretmeni",
        "Türkçe Öğretmeni",
        "Matematik Öğretmeni",
        "Fen Bilimleri Öğretmeni",
        "Sosyal Bilgiler Öğretmeni",
        "İngilizce Öğretmeni",
        "Beden Eğitimi Öğretmeni",
        "Görsel Sanatlar Öğretmeni",
        "Müzik Öğretmeni",
        "Rehber Öğretmen",
        "Bilişim Teknolojileri Öğretmeni",
        "Okul Öncesi Öğretmeni"
    };

    private static readonly string[] Universities =
    {
        "Marmara Üniversitesi",
        "İstanbul Üniversitesi",
        "Yıldız Teknik Üniversitesi",
        "Anadolu Üniversitesi",
        "Dokuz Eylül Üniversitesi",
        "Gazi Üniversitesi"
    };

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

        var locations = await EnsureLocationsAsync(dbContext, now);
        var definitions = BuildUserDefinitions();
        ValidateDefinitions(definitions);

        var usersByEmail = await UpsertUsersAsync(dbContext, definitions, now);

        await SynchronizeLocationAssignmentsAsync(dbContext, definitions, usersByEmail, locations, now);
        await EnsureChildrenAsync(dbContext, definitions, usersByEmail, now);
        await EnsureLeaveAgreementsAsync(dbContext, definitions, usersByEmail, now);

        var leaveTypesByCode = await EnsureLeaveTypesAsync(dbContext, now);
        var insertedLeaveCount = await EnsureSampleLeavesAsync(
            dbContext,
            definitions,
            usersByEmail,
            leaveTypesByCode,
            now);

        await dbContext.SaveChangesAsync();

        var verification = await VerifySeededDataAsync(
            dbContext,
            definitions,
            usersByEmail,
            BuildLeaveScenarios());

        logger.LogInformation(
            "Dummy test verileri doğrulandı. Kişi: {UserCount}, okul müdürü: {ManagerCount}, öğretmen: {TeacherCount}, konum ataması: {LocationAssignmentCount}, çocuk: {ChildCount}, izin mutabakatı: {AgreementCount}, izin talebi: {LeaveCount}, bu çalıştırmada eklenen izin: {InsertedLeaveCount}, son onaylayıcı: {FinalApproverEmail}",
            verification.UserCount,
            verification.ManagerCount,
            verification.TeacherCount,
            verification.LocationAssignmentCount,
            verification.ChildCount,
            verification.AgreementCount,
            verification.LeaveCount,
            insertedLeaveCount,
            FinalApproverEmail);
    }

    private static List<SeedUserDefinition> BuildUserDefinitions()
    {
        var users = new List<SeedUserDefinition>
        {
            new(
                0,
                FinalApproverEmail,
                "Mustafa",
                "Meral",
                "Genel Müdürlük Onay Yetkilisi",
                IsManager: false,
                IsTeacher: false,
                LocationIndex: TestSchoolNames.Length)
        };

        for (var index = 0; index < PrincipalNames.Length; index++)
        {
            var name = PrincipalNames[index];
            users.Add(new SeedUserDefinition(
                index + 1,
                index == 0 ? ManagerEmail : $"mudur{index + 1}.test@example.invalid",
                name.FirstName,
                name.LastName,
                "Okul Müdürü",
                IsManager: true,
                IsTeacher: false,
                LocationIndex: index));
        }

        for (var index = 0; index < TeacherNames.Length; index++)
        {
            var name = TeacherNames[index];
            users.Add(new SeedUserDefinition(
                index + PrincipalNames.Length + 1,
                TeacherEmailFor(index),
                name.FirstName,
                name.LastName,
                TeacherTitles[index % TeacherTitles.Length],
                IsManager: false,
                IsTeacher: true,
                LocationIndex: ((index * 7) + 1) % TestSchoolNames.Length));
        }

        return users;
    }

    private static void ValidateDefinitions(IReadOnlyCollection<SeedUserDefinition> definitions)
    {
        if (definitions.Count != ExpectedUserCount)
        {
            throw new InvalidOperationException(
                $"Dummy kullanıcı sayısı {ExpectedUserCount} olmalı; mevcut sayı: {definitions.Count}.");
        }

        if (definitions.Count(x => x.IsManager) != TestSchoolNames.Length)
        {
            throw new InvalidOperationException("Her dummy test okulunda tam olarak bir okul müdürü bulunmalıdır.");
        }

        if (definitions.Count(x => x.IsTeacher) != 35)
        {
            throw new InvalidOperationException("Dummy veri setinde 35 öğretmen bulunmalıdır.");
        }

        var duplicateEmail = definitions
            .GroupBy(x => x.Email, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Count() > 1);

        if (duplicateEmail != null)
        {
            throw new InvalidOperationException($"Tekrarlanan dummy e-posta adresi: {duplicateEmail.Key}");
        }
    }

    private static async Task<IReadOnlyList<Location>> EnsureLocationsAsync(
        ApplicationDbContext dbContext,
        DateTime now)
    {
        var locationNames = TestSchoolNames.Append(TestHeadOfficeName).ToArray();
        var existingLocations = await dbContext.Locations
            .Where(x => locationNames.Contains(x.Name))
            .ToListAsync();
        var locationsByName = existingLocations.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var name in locationNames)
        {
            if (locationsByName.ContainsKey(name))
            {
                continue;
            }

            var location = new Location
            {
                Name = name,
                CreatedDate = now,
                UpdateDate = now
            };
            dbContext.Locations.Add(location);
            locationsByName[name] = location;
        }

        await dbContext.SaveChangesAsync();
        return locationNames.Select(x => locationsByName[x]).ToList();
    }

    private static async Task<Dictionary<string, EmployeePortal>> UpsertUsersAsync(
        ApplicationDbContext dbContext,
        IReadOnlyCollection<SeedUserDefinition> definitions,
        DateTime now)
    {
        var emails = definitions.Select(x => x.Email).ToArray();
        var existingUsers = await dbContext.EmployeePortals
            .Where(x => emails.Contains(x.Email))
            .ToListAsync();
        var usersByEmail = existingUsers.ToDictionary(x => x.Email, StringComparer.OrdinalIgnoreCase);

        foreach (var definition in definitions)
        {
            var isNewUser = false;
            if (!usersByEmail.TryGetValue(definition.Email, out var user))
            {
                isNewUser = true;
                user = new EmployeePortal
                {
                    Email = definition.Email,
                    CreatedDate = now
                };
                dbContext.EmployeePortals.Add(user);
                usersByEmail[definition.Email] = user;
            }

            var childCount = GetChildCount(definition.Sequence);
            var hireYears = 1 + ((definition.Sequence * 5) % 18);
            var age = 26 + ((definition.Sequence * 7) % 32);

            user.FirstName = definition.FirstName;
            user.LastName = definition.LastName;
            user.PhoneNumber = $"0555100{definition.Sequence + 1:0000}";
            user.Title = definition.Title;
            user.HireDate = now.Date
                .AddYears(-hireYears)
                .AddDays(-((definition.Sequence * 17) % 280));
            user.TerminationDate = null;
            user.BirthDate = now.Date
                .AddYears(-age)
                .AddDays(-((definition.Sequence * 13) % 250));
            user.AddressText = $"Test Mahallesi, Dummy Sokak No: {definition.Sequence + 1}, İstanbul";
            user.MaritalStatus = childCount > 0 || definition.Sequence % 2 == 0
                ? MaritalStatusType.Married
                : MaritalStatusType.Single;
            user.EducationUniversity = Universities[definition.Sequence % Universities.Length];
            user.EducationFaculty = definition.IsTeacher ? "Eğitim Fakültesi" : "İktisadi ve İdari Bilimler Fakültesi";
            user.EducationDepartment = definition.IsTeacher ? definition.Title.Replace(" Öğretmeni", string.Empty) : "Eğitim Yönetimi";
            if (isNewUser)
            {
                user.LeaveDays = 12m + (definition.Sequence % 17) + (definition.Sequence % 5 == 0 ? 0.5m : 0m);
            }

            user.IsAdmin = false;
            user.IsManager = definition.IsManager;
            user.IsDeleted = false;
            user.UpdateDate = now;
        }

        await dbContext.SaveChangesAsync();
        return usersByEmail;
    }

    private static async Task SynchronizeLocationAssignmentsAsync(
        ApplicationDbContext dbContext,
        IReadOnlyCollection<SeedUserDefinition> definitions,
        IReadOnlyDictionary<string, EmployeePortal> usersByEmail,
        IReadOnlyList<Location> locations,
        DateTime now)
    {
        var userIds = usersByEmail.Values.Select(x => x.Id).ToArray();
        var existingAssignments = await dbContext.EmployeePortalLocations
            .Where(x => userIds.Contains(x.EmployeePortalId))
            .ToListAsync();
        var definitionsByUserId = definitions.ToDictionary(x => usersByEmail[x.Email].Id);

        foreach (var assignment in existingAssignments)
        {
            var expectedLocationId = locations[definitionsByUserId[assignment.EmployeePortalId].LocationIndex].Id;
            if (assignment.LocationId != expectedLocationId)
            {
                dbContext.EmployeePortalLocations.Remove(assignment);
            }
        }

        var currentAssignments = existingAssignments
            .Where(x => dbContext.Entry(x).State != EntityState.Deleted)
            .Select(x => (x.EmployeePortalId, x.LocationId))
            .ToHashSet();

        foreach (var definition in definitions)
        {
            var user = usersByEmail[definition.Email];
            var location = locations[definition.LocationIndex];
            if (currentAssignments.Contains((user.Id, location.Id)))
            {
                continue;
            }

            dbContext.EmployeePortalLocations.Add(new EmployeePortalLocation
            {
                EmployeePortalId = user.Id,
                LocationId = location.Id,
                CreatedDate = now,
                UpdateDate = now
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureChildrenAsync(
        ApplicationDbContext dbContext,
        IReadOnlyCollection<SeedUserDefinition> definitions,
        IReadOnlyDictionary<string, EmployeePortal> usersByEmail,
        DateTime now)
    {
        var userIds = usersByEmail.Values.Select(x => x.Id).ToArray();
        var usersWithChildren = (await dbContext.EmployeePortalChildren
                .Where(x => userIds.Contains(x.EmployeePortalId))
                .Select(x => x.EmployeePortalId)
                .Distinct()
                .ToListAsync())
            .ToHashSet();

        foreach (var definition in definitions)
        {
            var user = usersByEmail[definition.Email];
            var childCount = GetChildCount(definition.Sequence);
            if (childCount == 0 || usersWithChildren.Contains(user.Id))
            {
                continue;
            }

            for (var childIndex = 0; childIndex < childCount; childIndex++)
            {
                var childAge = 3 + ((definition.Sequence * 3 + childIndex * 7) % 19);
                dbContext.EmployeePortalChildren.Add(new EmployeePortalChild
                {
                    EmployeePortalId = user.Id,
                    Gender = (definition.Sequence + childIndex) % 2 == 0
                        ? ChildGenderType.Female
                        : ChildGenderType.Male,
                    BirthDate = now.Date
                        .AddYears(-childAge)
                        .AddDays(-((definition.Sequence * 19 + childIndex * 31) % 240)),
                    EducationStatus = GetEducationStatus(childAge),
                    CreatedDate = now,
                    UpdateDate = now
                });
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureLeaveAgreementsAsync(
        ApplicationDbContext dbContext,
        IReadOnlyCollection<SeedUserDefinition> definitions,
        IReadOnlyDictionary<string, EmployeePortal> usersByEmail,
        DateTime now)
    {
        var userIds = usersByEmail.Values.Select(x => x.Id).ToArray();
        var existingUserIds = (await dbContext.LeaveAgreements
                .Where(x => userIds.Contains(x.EmployeePortalId))
                .Select(x => x.EmployeePortalId)
                .ToListAsync())
            .ToHashSet();
        var balanceDate = new DateTime(now.Year - 1, 12, 31);

        foreach (var definition in definitions)
        {
            var user = usersByEmail[definition.Email];
            if (existingUserIds.Contains(user.Id))
            {
                continue;
            }

            dbContext.LeaveAgreements.Add(new LeaveAgreement
            {
                EmployeePortalId = user.Id,
                AgreedLeaveDays = 10m + (definition.Sequence % 16),
                BalanceAsOfDate = balanceDate,
                CurrentYearEarnedDays = definition.Sequence % 3 == 0 ? 14m : 0m,
                CurrentYearUsedDays = definition.Sequence % 4,
                IsSigned = definition.Sequence % 3 != 0,
                CreatedDate = now,
                UpdateDate = now
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task<Dictionary<string, LeaveType>> EnsureLeaveTypesAsync(
        ApplicationDbContext dbContext,
        DateTime now)
    {
        var definitions = new (string Code, string Name, bool IsActive)[]
        {
            (LeaveTypeCodes.Annual, "Yıllık İzin", true),
            (LeaveTypeCodes.Excuse, "Mazeret İzni", false),
            (LeaveTypeCodes.Other, "Diğer", true),
            (LeaveTypeCodes.Unpaid, "Ücretsiz İzin", true),
            (LeaveTypeCodes.Sick, "Hastalık İzni", true),
            (LeaveTypeCodes.Maternity, "Doğum İzni", true),
            (LeaveTypeCodes.Paternity, "Babalık İzni", true),
            (LeaveTypeCodes.Marriage, "Evlilik İzni", true),
            (LeaveTypeCodes.Bereavement, "Ölüm İzni", true)
        };
        var codes = definitions.Select(x => x.Code).ToArray();
        var existingTypes = await dbContext.LeaveTypes
            .Where(x => codes.Contains(x.Code))
            .ToListAsync();
        var typesByCode = existingTypes.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var definition in definitions)
        {
            if (typesByCode.ContainsKey(definition.Code))
            {
                continue;
            }

            var leaveType = new LeaveType
            {
                Code = definition.Code,
                Name = definition.Name,
                IsActive = definition.IsActive,
                CreatedDate = now,
                UpdateDate = now
            };
            dbContext.LeaveTypes.Add(leaveType);
            typesByCode[definition.Code] = leaveType;
        }

        await dbContext.SaveChangesAsync();
        return typesByCode;
    }

    private static async Task<int> EnsureSampleLeavesAsync(
        ApplicationDbContext dbContext,
        IReadOnlyCollection<SeedUserDefinition> definitions,
        IReadOnlyDictionary<string, EmployeePortal> usersByEmail,
        IReadOnlyDictionary<string, LeaveType> leaveTypesByCode,
        DateTime now)
    {
        var scenarios = BuildLeaveScenarios();
        var reasons = scenarios.Select(x => x.Reason).ToArray();
        var existingLeavesByReason = (await dbContext.Leaves
                .Where(x => reasons.Contains(x.Reason))
                .ToListAsync())
            .GroupBy(x => x.Reason, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
        var definitionsByEmail = definitions.ToDictionary(x => x.Email, StringComparer.OrdinalIgnoreCase);
        var principalsByLocation = definitions
            .Where(x => x.IsManager)
            .ToDictionary(x => x.LocationIndex);
        var insertedCount = 0;

        foreach (var scenario in scenarios)
        {
            if (existingLeavesByReason.TryGetValue(scenario.Reason, out var existingLeave))
            {
                if (scenario.Sequence == 8 && existingLeave.Status == (int)LeaveStatus.PendingFinalApproval)
                {
                    existingLeave.ManagerDecisionBy = null;
                    existingLeave.ManagerDecisionDate = null;
                    existingLeave.UpdateDate = now;
                }

                continue;
            }

            var definition = definitionsByEmail[scenario.EmployeeEmail];
            var employee = usersByEmail[scenario.EmployeeEmail];
            var principal = principalsByLocation.GetValueOrDefault(definition.LocationIndex);
            var principalName = principal == null
                ? "Okul Müdürü"
                : $"{principal.FirstName} {principal.LastName}";
            var startDate = MoveToWeekday(now.Date.AddDays(scenario.StartOffsetDays)).AddHours(9);
            var endDate = startDate.Date.AddDays(scenario.CalendarDaySpan - 1).AddHours(18);
            var requestedDays = LeaveDurationCalculator.CalculateRequestedDays(startDate, endDate);
            var requestCreatedAt = scenario.StartOffsetDays < 0
                ? startDate.AddDays(-7)
                : now.AddDays(-(scenario.Sequence % 9 + 1));

            var leave = new MEC.Domain.Entity.Leave.Leave
            {
                EmployeeId = employee.Id,
                StartDate = startDate,
                EndDate = endDate,
                LeaveTypeId = leaveTypesByCode[scenario.LeaveTypeCode].Id,
                RequestedDays = requestedDays,
                RemainingLeaveDays = Math.Max(
                    0m,
                    employee.LeaveDays - (scenario.Status == LeaveStatus.Approved &&
                                           scenario.LeaveTypeCode == LeaveTypeCodes.Annual
                        ? requestedDays
                        : 0m)),
                MinimumBlockExceptionRequested = scenario.LeaveTypeCode == LeaveTypeCodes.Annual && requestedDays < 6m,
                Reason = scenario.Reason,
                Status = (int)scenario.Status,
                CreatedDate = requestCreatedAt,
                UpdateDate = requestCreatedAt
            };

            ApplyDecisionHistory(leave, scenario, employee, principalName, requestCreatedAt);
            dbContext.Leaves.Add(leave);
            existingLeavesByReason[scenario.Reason] = leave;
            insertedCount++;
        }

        return insertedCount;
    }

    private static async Task<SeedVerificationResult> VerifySeededDataAsync(
        ApplicationDbContext dbContext,
        IReadOnlyCollection<SeedUserDefinition> definitions,
        IReadOnlyDictionary<string, EmployeePortal> usersByEmail,
        IReadOnlyCollection<SeedLeaveScenario> leaveScenarios)
    {
        var userIds = usersByEmail.Values.Select(x => x.Id).ToArray();
        var managerIds = definitions
            .Where(x => x.IsManager)
            .Select(x => usersByEmail[x.Email].Id)
            .ToArray();
        var teacherIds = definitions
            .Where(x => x.IsTeacher)
            .Select(x => usersByEmail[x.Email].Id)
            .ToArray();
        var leaveReasons = leaveScenarios.Select(x => x.Reason).ToArray();

        var result = new SeedVerificationResult(
            await dbContext.EmployeePortals.CountAsync(x => userIds.Contains(x.Id) && !x.IsDeleted),
            await dbContext.EmployeePortals.CountAsync(x => managerIds.Contains(x.Id) && x.IsManager && !x.IsDeleted),
            await dbContext.EmployeePortals.CountAsync(x => teacherIds.Contains(x.Id) && !x.IsManager && !x.IsDeleted),
            await dbContext.EmployeePortalLocations.CountAsync(x => userIds.Contains(x.EmployeePortalId)),
            await dbContext.EmployeePortalChildren.CountAsync(x => userIds.Contains(x.EmployeePortalId)),
            await dbContext.LeaveAgreements.CountAsync(x => userIds.Contains(x.EmployeePortalId)),
            await dbContext.Leaves.CountAsync(x => leaveReasons.Contains(x.Reason)));

        if (result.UserCount != ExpectedUserCount ||
            result.ManagerCount != TestSchoolNames.Length ||
            result.TeacherCount != TeacherNames.Length ||
            result.LocationAssignmentCount != ExpectedUserCount ||
            result.AgreementCount != ExpectedUserCount ||
            result.LeaveCount != leaveScenarios.Count)
        {
            throw new InvalidOperationException(
                $"Dummy veri doğrulaması başarısız. Kişi={result.UserCount}, Müdür={result.ManagerCount}, " +
                $"Öğretmen={result.TeacherCount}, Konum={result.LocationAssignmentCount}, " +
                $"Mutabakat={result.AgreementCount}, İzin={result.LeaveCount}.");
        }

        return result;
    }

    private static IReadOnlyList<SeedLeaveScenario> BuildLeaveScenarios()
    {
        return new List<SeedLeaveScenario>
        {
            new(1, TeacherEmailFor(0), LeaveTypeCodes.Annual, 8, 3, LeaveStatus.Pending, "[DUMMY-IZIN-01] Aile ziyareti için yıllık izin"),
            new(2, TeacherEmailFor(4), LeaveTypeCodes.Sick, 4, 1, LeaveStatus.Pending, "[DUMMY-IZIN-02] Kontrol ve tedavi randevusu"),
            new(3, TeacherEmailFor(8), LeaveTypeCodes.Marriage, 16, 3, LeaveStatus.Pending, "[DUMMY-IZIN-03] Evlilik izni talebi"),
            new(4, TeacherEmailFor(12), LeaveTypeCodes.Other, 12, 1, LeaveStatus.Pending, "[DUMMY-IZIN-04] Resmî kurum işlemleri"),

            new(5, TeacherEmailFor(1), LeaveTypeCodes.Annual, 18, 5, LeaveStatus.PendingFinalApproval, "[DUMMY-IZIN-05] Yaz dönemi yıllık izin planı"),
            new(6, TeacherEmailFor(5), LeaveTypeCodes.Paternity, 7, 5, LeaveStatus.PendingFinalApproval, "[DUMMY-IZIN-06] Babalık izni talebi"),
            new(7, TeacherEmailFor(9), LeaveTypeCodes.Unpaid, 25, 4, LeaveStatus.PendingFinalApproval, "[DUMMY-IZIN-07] Şehir dışı aile işleri"),
            new(8, ManagerEmail, LeaveTypeCodes.Annual, 20, 3, LeaveStatus.PendingFinalApproval, "[DUMMY-IZIN-08] Okul müdürü yıllık izin talebi"),

            new(9, TeacherEmailFor(2), LeaveTypeCodes.Annual, -45, 4, LeaveStatus.Approved, "[DUMMY-IZIN-09] Tamamlanmış yıllık izin"),
            new(10, TeacherEmailFor(6), LeaveTypeCodes.Sick, -30, 2, LeaveStatus.Approved, "[DUMMY-IZIN-10] Geçmiş hastalık izni"),
            new(11, TeacherEmailFor(10), LeaveTypeCodes.Maternity, -120, 30, LeaveStatus.Approved, "[DUMMY-IZIN-11] Doğum izni kaydı"),
            new(12, TeacherEmailFor(14), LeaveTypeCodes.Annual, -75, 8, LeaveStatus.Approved, "[DUMMY-IZIN-12] Geçmiş dönem yıllık izni"),
            new(13, TeacherEmailFor(18), LeaveTypeCodes.Bereavement, -20, 3, LeaveStatus.Approved, "[DUMMY-IZIN-13] Yakın vefatı nedeniyle izin"),

            new(14, TeacherEmailFor(3), LeaveTypeCodes.Annual, 14, 2, LeaveStatus.Rejected, "[DUMMY-IZIN-14] Yoğun dönem yıllık izin talebi", RejectedByFinalApprover: false),
            new(15, TeacherEmailFor(7), LeaveTypeCodes.Unpaid, 28, 6, LeaveStatus.Rejected, "[DUMMY-IZIN-15] Ücretsiz izin talebi", RejectedByFinalApprover: true),
            new(16, TeacherEmailFor(11), LeaveTypeCodes.Other, 10, 1, LeaveStatus.Rejected, "[DUMMY-IZIN-16] Kişisel mazeret talebi", RejectedByFinalApprover: false),

            new(17, TeacherEmailFor(15), LeaveTypeCodes.Annual, 30, 3, LeaveStatus.Cancelled, "[DUMMY-IZIN-17] Çalışan tarafından iptal edilen yıllık izin"),
            new(18, TeacherEmailFor(19), LeaveTypeCodes.Sick, 6, 1, LeaveStatus.Cancelled, "[DUMMY-IZIN-18] İptal edilen sağlık izni")
        };
    }

    private static void ApplyDecisionHistory(
        MEC.Domain.Entity.Leave.Leave leave,
        SeedLeaveScenario scenario,
        EmployeePortal employee,
        string principalName,
        DateTime requestCreatedAt)
    {
        var managerDecisionDate = requestCreatedAt.AddHours(6);
        var finalDecisionDate = managerDecisionDate.AddHours(5);

        switch (scenario.Status)
        {
            case LeaveStatus.PendingFinalApproval:
                if (!employee.IsManager)
                {
                    leave.ManagerDecisionBy = principalName;
                    leave.ManagerDecisionDate = managerDecisionDate;
                    leave.UpdateDate = managerDecisionDate;
                }
                break;
            case LeaveStatus.Approved:
                leave.ManagerDecisionBy = principalName;
                leave.ManagerDecisionDate = managerDecisionDate;
                leave.DecisionBy = "Mustafa Meral";
                leave.DecisionDate = finalDecisionDate;
                leave.UpdateDate = finalDecisionDate;
                break;
            case LeaveStatus.Rejected when scenario.RejectedByFinalApprover:
                leave.ManagerDecisionBy = principalName;
                leave.ManagerDecisionDate = managerDecisionDate;
                leave.DecisionBy = "Mustafa Meral";
                leave.DecisionDate = finalDecisionDate;
                leave.UpdateDate = finalDecisionDate;
                break;
            case LeaveStatus.Rejected:
                leave.ManagerDecisionBy = principalName;
                leave.ManagerDecisionDate = managerDecisionDate;
                leave.DecisionBy = principalName;
                leave.DecisionDate = managerDecisionDate;
                leave.UpdateDate = managerDecisionDate;
                break;
            case LeaveStatus.Cancelled:
                leave.DecisionBy = $"{employee.FirstName} {employee.LastName}";
                leave.DecisionDate = managerDecisionDate;
                leave.UpdateDate = managerDecisionDate;
                break;
        }
    }

    private static int GetChildCount(int sequence)
    {
        if (sequence == 0)
        {
            return 0;
        }

        if (sequence % 4 == 0)
        {
            return 2;
        }

        return sequence % 3 == 0 ? 1 : 0;
    }

    private static ChildEducationStatusType GetEducationStatus(int age)
    {
        return age switch
        {
            <= 3 => ChildEducationStatusType.NotInEducation,
            <= 5 => ChildEducationStatusType.Preschool,
            <= 9 => ChildEducationStatusType.PrimarySchool,
            <= 13 => ChildEducationStatusType.MiddleSchool,
            <= 17 => ChildEducationStatusType.HighSchool,
            _ => ChildEducationStatusType.Undergraduate
        };
    }

    private static DateTime MoveToWeekday(DateTime date)
    {
        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            date = date.AddDays(1);
        }

        return date;
    }

    private static string TeacherEmailFor(int zeroBasedIndex)
    {
        return zeroBasedIndex == 0
            ? TeacherEmail
            : $"ogretmen{zeroBasedIndex + 1:00}.test@example.invalid";
    }

    private sealed record SeedUserDefinition(
        int Sequence,
        string Email,
        string FirstName,
        string LastName,
        string Title,
        bool IsManager,
        bool IsTeacher,
        int LocationIndex);

    private sealed record SeedLeaveScenario(
        int Sequence,
        string EmployeeEmail,
        string LeaveTypeCode,
        int StartOffsetDays,
        int CalendarDaySpan,
        LeaveStatus Status,
        string Reason,
        bool RejectedByFinalApprover = false);

    private sealed record SeedVerificationResult(
        int UserCount,
        int ManagerCount,
        int TeacherCount,
        int LocationAssignmentCount,
        int ChildCount,
        int AgreementCount,
        int LeaveCount);
}
