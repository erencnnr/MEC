using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;


var builder = WebApplication.CreateBuilder(args);

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
