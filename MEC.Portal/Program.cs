using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.Application.Abstractions.Service.LeaveService;
using MEC.Application.Abstractions.Service.LoggingService;
using MEC.Application.Abstractions.Service.LoginService;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Application.Service.EmployeeService;
using MEC.Application.Service.LoggingService;
using MEC.Application.Service.LoginService;
using MEC.Application.Service.SchoolService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.DAL.Config.Applicaiton.EntityFramework;
using MEC.DAL.Config.Contexts;
using MEC.Portal.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
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
    var connectionString = builder.Configuration.GetConnectionString(environment == "Test" ? "DefaultConnection" : "ProdConnection");

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

    builder.Services.AddScoped<ILoginService, LoginService>();
    builder.Services.AddScoped<IEmployeePortalService, EmployeePortalService>();
    builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
    builder.Services.AddScoped<ILibraryService, LibraryService>();
    builder.Services.AddScoped<ISliderService, SliderService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<ILeaveService, LeaveService>();
    builder.Services.AddScoped<IApiLogService, ApiLogService>();
    builder.Services.AddScoped<IUserActionLogService, UserActionLogService>();

    builder.Services.AddHttpClient<IAttachmentApiClient, AttachmentApiClient>((serviceProvider, client) =>
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var baseUrl = configuration["WebApi:BaseUrl"];

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            client.BaseAddress = new Uri(baseUrl);
        }
    });

    builder.Services.AddHttpClient<IAnnouncementAttachmentApiClient, AnnouncementAttachmentApiClient>((serviceProvider, client) =>
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var baseUrl = configuration["WebApi:BaseUrl"];

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            client.BaseAddress = new Uri(baseUrl);
        }
    });

    builder.Services.AddHttpClient<IAnnouncementImageApiClient, AnnouncementImageApiClient>((serviceProvider, client) =>
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var baseUrl = configuration["WebApi:BaseUrl"];

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            client.BaseAddress = new Uri(baseUrl);
        }
    });

    builder.Services.AddHttpClient<ISliderImageApiClient, SliderImageApiClient>((serviceProvider, client) =>
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var baseUrl = configuration["WebApi:BaseUrl"];

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            client.BaseAddress = new Uri(baseUrl);
        }
    });

    builder.Services.AddHttpClient<ILibraryAttachmentApiClient, LibraryAttachmentApiClient>((serviceProvider, client) =>
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var baseUrl = configuration["WebApi:BaseUrl"];

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            client.BaseAddress = new Uri(baseUrl);
        }
    });

    builder.Services.AddHttpClient<IPortalUserSyncApiClient, PortalUserSyncApiClient>((serviceProvider, client) =>
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var baseUrl = configuration["WebApi:BaseUrl"];

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            client.BaseAddress = new Uri(baseUrl);
        }
    });

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
