using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using MEC.Application.Service.SchoolService;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.DAL.Config.Applicaiton.EntityFramework;
using MEC.DAL.Config.Contexts;
using Microsoft.EntityFrameworkCore;
using MEC.Application.Abstractions.Service.AssetService;
using MEC.Application.Service.AssetService;
using MEC.Application.Abstractions.Service.InvoiceService;
using MEC.Application.Abstractions.Service.LoanService;
using MEC.Application.Service.InvoiceService;
using MEC.Application.Service.LoanService;
using MEC.Application.Service.EmployeeService; 
using MEC.Application.Service.LoginService; 
using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.Application.Abstractions.Service.LoginService;
using MEC.Application.Abstractions.Service.ServiceHistoryService;
using MEC.Application.Service.ServiceHistoryService;
using MEC.AssetManagementUI.Services;

var builder = WebApplication.CreateBuilder(args);
var environment = builder.Configuration["AppSettings:Environment"];
var connectionString = builder.Configuration.GetConnectionString(environment == "Test" ? "DefaultConnection" : "ProdConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Generic Repository ve Servis Kayıtları
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<ISchoolService, SchoolService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IAssetTypeService, AssetTypeService>();
builder.Services.AddScoped<IAssetStatusService, AssetStatusService>();
builder.Services.AddScoped<IAssetImageService, AssetImageService>();
builder.Services.AddScoped<IAssetAttachmentService, AssetAttachmentService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<ILoanStatusService, LoanStatusService>();
builder.Services.AddScoped<ISchoolClassService, SchoolClassService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IEmployeePortalService, EmployeePortalService>();
builder.Services.AddScoped<IEmployeeTypeService, EmployeeTypeService>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IServiceHistoryService, ServiceHistoryService>();
builder.Services.AddHttpClient<IAssetImageApiClient, AssetImageApiClient>(ConfigureWebApiClient);
builder.Services.AddHttpClient<IAssetAttachmentApiClient, AssetAttachmentApiClient>(ConfigureWebApiClient);

// MVC Servisleri
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser() // Herkes login olmak zorunda
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy)); // Filtreyi ekle
});

// 2. Cookie Authentication Ayarları
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Giriş yapmamış kullanıcı buraya atılır
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20); // Oturum süresi
    });

var app = builder.Build();

// ... Diğer middleware ayarları (Hsts, HttpsRedirection vs.) ...

app.UseStaticFiles();

app.UseRouting();

// 3. Bu sıralama ÇOK ÖNEMLİ: UseRouting'den sonra, UseAuthorization'dan önce olmalı.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Asset}/{action=Index}/{id?}");

app.Run();

static void ConfigureWebApiClient(IServiceProvider serviceProvider, HttpClient httpClient)
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["WebApi:BaseUrl"] ?? "https://localhost:7103/";

    if (!string.IsNullOrWhiteSpace(baseUrl) &&
        Uri.TryCreate(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/", UriKind.Absolute, out var baseAddress))
    {
        httpClient.BaseAddress = baseAddress;
    }
}

