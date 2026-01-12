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

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Generic Repository ve Servis Kayýtlarý
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
builder.Services.AddScoped<IEmployeeTypeService, EmployeeTypeService>();
builder.Services.AddScoped<ILoginService, LoginService>();

// MVC Servisleri
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser() // Herkes login olmak zorunda
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy)); // Filtreyi ekle
});

// 2. Cookie Authentication Ayarlarý
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Giriþ yapmamýþ kullanýcý buraya atýlýr
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20); // Oturum süresi
    });

var app = builder.Build();

// ... Diðer middleware ayarlarý (Hsts, HttpsRedirection vs.) ...

app.UseStaticFiles();

app.UseRouting();

// 3. Bu sýralama ÇOK ÖNEMLÝ: UseRouting'den sonra, UseAuthorization'dan önce olmalý.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Asset}/{action=Index}/{id?}");

app.Run();
