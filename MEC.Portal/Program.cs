using Microsoft.AspNetCore.Authentication.Cookies;

// 1. ADIM: Önce Builder oluþturulur (En baþa bunu alýyoruz)
var builder = WebApplication.CreateBuilder(args);

// 2. ADIM: Servisler eklenir (Artýk builder var olduðu için hata vermez)
builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Giriþ sayfasý yolu
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20); // Oturum süresi
    });

// 3. ADIM: Uygulama inþa edilir (Build)
var app = builder.Build();

// 4. ADIM: Uygulama ayarlarý (Middleware)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Resim, CSS vb. dosyalar için gerekli standart kod

app.UseRouting();

// BU ÝKÝLÝNÝN YERÝ ÇOK ÖNEMLÝ (Routing'den SONRA gelmeli)
app.UseAuthentication(); // Kimlik kontrolü
app.UseAuthorization();  // Yetki kontrolü

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();