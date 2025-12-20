using MEC.AssetManagementUI.Services;
using Microsoft.AspNetCore.Authentication.Cookies;


var builder = WebApplication.CreateBuilder(args);

// MVC Servisleri
builder.Services.AddControllersWithViews();

// 1. Dependency Injection Tanýmlamasý
// Ýleride veritabaný servisi yazýnca sadece "MockAuthService" kýsmýný "DbAuthService" yapacaksýnýz.
builder.Services.AddScoped<IAuthService, MockAuthService>();

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
