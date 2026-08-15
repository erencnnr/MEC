using MEC.Application.Abstractions.Service.ApprovalWorkflowService;
using MEC.Application.Abstractions.Service.ApprovalWorkflowService.Model;
using MEC.Application.Abstractions.Service.AssetService;
using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.Application.Abstractions.Service.LeaveService;
using MEC.Application.Abstractions.Service.LoanService;
using MEC.Application.Abstractions.Service.LoggingService;
using MEC.Application.Abstractions.Service.LoginService;
using MEC.Application.Abstractions.Service.NotificationService;
using MEC.Application.Abstractions.Service.OvertimeService;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Application.Abstractions.Service.ServiceHistoryService;
using MEC.Application.Service.ApprovalWorkflowService;
using MEC.Application.Service.AssetService;
using MEC.Application.Service.EmployeeService;
using MEC.Application.Service.OvertimeService;
using MEC.Application.Service.LoanService;
using MEC.Application.Service.LoggingService;
using MEC.Application.Service.LoginService;
using MEC.Application.Service.SchoolService;
using MEC.Application.Service.ServiceHistoryService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.DAL.Config.Applicaiton.EntityFramework;
using MEC.DAL.Config.Contexts;
using MEC.Portal.Middleware;
using MEC.Portal.Options;
using MEC.Portal.Services;
using MEC.Portal.Services.Notifications;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .WriteTo.Console());

    var environment = builder.Configuration["AppSettings:Environment"];
    var connectionString = EnsureMySqlConnectionString(
        builder.Configuration.GetConnectionString(environment == "Test" ? "DefaultConnection" : "ProdConnection"));

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

    builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

    builder.Services.AddControllersWithViews(options =>
    {
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
        options.Filters.Add(new AuthorizeFilter(policy));
    });
    builder.Services.AddMemoryCache();
    builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
    builder.Services.Configure<NotificationOptions>(builder.Configuration.GetSection("NotificationOptions"));
    builder.Services.Configure<PortalUrlOptions>(builder.Configuration.GetSection("PortalUrl"));
    builder.Services.AddSingleton(
        builder.Configuration.GetSection("ApprovalWorkflow").Get<ApprovalWorkflowSettings>()
        ?? new ApprovalWorkflowSettings());

    builder.Services.AddScoped<ILoginService, LoginService>();
    builder.Services.AddScoped<IEmployeePortalService, EmployeePortalService>();
    builder.Services.AddScoped<IApprovalWorkflowService, ApprovalWorkflowService>();
    builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
    builder.Services.AddScoped<ILibraryService, LibraryService>();
    builder.Services.AddScoped<ISliderService, SliderService>();
    builder.Services.AddScoped<IBirthdayPopupService, BirthdayPopupService>();
    builder.Services.AddScoped<IFoodMenuService, FoodMenuService>();
    builder.Services.AddScoped<ISurveyService, SurveyService>();
    builder.Services.AddScoped<IWorkflowNotificationService, SmtpWorkflowNotificationService>();
    builder.Services.AddScoped<ILeaveService, LeaveService>();
    builder.Services.AddScoped<IOvertimeService, OvertimeService>();
    builder.Services.AddScoped<IApiLogService, ApiLogService>();
    builder.Services.AddScoped<IUserActionLogService, UserActionLogService>();
    builder.Services.AddScoped<IAssetService, AssetService>();
    builder.Services.AddScoped<IAssetTypeService, AssetTypeService>();
    builder.Services.AddScoped<IAssetStatusService, AssetStatusService>();
    builder.Services.AddScoped<IAssetImageService, AssetImageService>();
    builder.Services.AddScoped<IAssetAttachmentService, AssetAttachmentService>();
    builder.Services.AddScoped<ILoanService, LoanService>();
    builder.Services.AddScoped<ILoanStatusService, LoanStatusService>();
    builder.Services.AddScoped<ISchoolService, SchoolService>();
    builder.Services.AddScoped<ISchoolClassService, SchoolClassService>();
    builder.Services.AddScoped<IServiceHistoryService, ServiceHistoryService>();

    builder.Services.AddHttpClient<IAttachmentApiClient, AttachmentApiClient>(ConfigureApiClient);
    builder.Services.AddHttpClient<IAnnouncementAttachmentApiClient, AnnouncementAttachmentApiClient>(ConfigureApiClient);
    builder.Services.AddHttpClient<IAnnouncementImageApiClient, AnnouncementImageApiClient>(ConfigureApiClient);
    builder.Services.AddHttpClient<ISliderImageApiClient, SliderImageApiClient>(ConfigureApiClient);
    builder.Services.AddHttpClient<IBirthdayPopupImageApiClient, BirthdayPopupImageApiClient>(ConfigureApiClient);
    builder.Services.AddHttpClient<ILibraryAttachmentApiClient, LibraryAttachmentApiClient>(ConfigureApiClient);
    builder.Services.AddHttpClient<IFoodMenuAttachmentApiClient, FoodMenuAttachmentApiClient>(ConfigureApiClient);
    builder.Services.AddHttpClient<IPortalUserSyncApiClient, PortalUserSyncApiClient>(ConfigureApiClient);
    builder.Services.AddHttpClient<IAssetImageApiClient, AssetImageApiClient>(ConfigureApiClient);
    builder.Services.AddHttpClient<IAssetAttachmentApiClient, AssetAttachmentApiClient>(ConfigureApiClient);

    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
        });

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();
    app.UseAuthentication();
    app.UseMiddleware<ProfileCompletionMiddleware>();
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "MEC.Portal host terminated unexpectedly.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

static void ConfigureApiClient(IServiceProvider serviceProvider, HttpClient client)
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["WebApi:BaseUrl"];

    if (!string.IsNullOrWhiteSpace(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl);
    }
}

static string EnsureMySqlConnectionString(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Database connection string is not configured.");
    }

    var builder = new MySqlConnectionStringBuilder(connectionString)
    {
        ConvertZeroDateTime = true
    };

    return builder.ConnectionString;
}
