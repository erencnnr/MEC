using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using MEC.Application.Service.SchoolService;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.DAL.Config.Applicaiton.EntityFramework;
using MEC.DAL.Config.Contexts;
using Microsoft.EntityFrameworkCore;
using MEC.Application.Service.EmployeeService; // Ekleyin
using MEC.Application.Abstractions.Service.EmployeeService; // Ekleyin

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<ISchoolService, SchoolService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

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
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
