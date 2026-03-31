using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.Application.Abstractions.Service.LeaveService;
using MEC.Application.Abstractions.Service.LoginService;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Application.Service.EmployeeService;
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


var builder = WebApplication.CreateBuilder(args);
var environment = builder.Configuration["AppSettings:Environment"];
var connectionString = builder.Configuration.GetConnectionString(environment == "Test" ? "DefaultConnection" : "ProdConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 2. Generic Repository Kaydı (Hatanın temel çözüm noktası)
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// Sisteme giren herkesin giriş yapmış olmasını zorunlu kılan filtre ayarı
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser() // Herkes login olmak zorunda
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy)); // Filtreyi tüm sisteme ekle
});

// Servis Kayıtları
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IEmployeePortalService, EmployeePortalService>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
// Cookie Authentication Ayarları
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// UseAuthentication ve UseAuthorization sıralaması
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();