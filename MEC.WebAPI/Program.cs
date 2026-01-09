using MEC.Application.Abstractions.Service.LdapService;
using MEC.Application.Service.LdapService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.DAL.Config.Applicaiton.EntityFramework;
using MEC.DAL.Config.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// --- SERVÝSLER ---

// 1. CORS Ayarý (TEK SEFERDE TANIMLA)
// IIS'te UI projeniz artýk "localhost:7255" olmayabilir (Domain adý veya sunucu IP'si olabilir).
// Baþlangýçta hata almamak için AllowAnyOrigin kullanýyoruz. 
// Canlýya alýrken "WithOrigins("https://alanadiniz.com")" olarak deðiþtirmek daha güvenlidir.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()  // Prod ortamýnda buraya UI domain'i yazýlmalý
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<ILdapService, LdapService>();

var app = builder.Build();

// --- MIDDLEWARE (SIRALAMA ÖNEMLÝDÝR) ---

// 2. Swagger Ayarý
// IIS'e attýðýnýzda ortam "Production" olur. Swagger varsayýlan olarak sadece "Development"ta çalýþýr.
// Swagger'ý IIS'te de görmek istiyorsanýz if bloðunu kaldýrýn veya || true ekleyin.
// (Güvenlik gereði canlý ortamda kapatýlmasý önerilir ama test için açabilirsiniz)
app.UseSwagger();
app.UseSwaggerUI();

// 3. CORS'u Aktif Et (En baþlarda olmalý)
app.UseCors("AllowAll");

app.UseHttpsRedirection();

// 4. Statik Dosya (Resim) Ayarlarý
// DÝKKAT: IIS'in C:\Images klasörüne eriþim izni olmasý þarttýr!
string imagePath = @"C:\Images";

// Klasör yoksa oluþtur
if (!Directory.Exists(imagePath))
{
    Directory.CreateDirectory(imagePath);
}

// Klasörü dýþarý aç
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imagePath),
    RequestPath = "/static"
});

app.UseAuthorization();

app.MapControllers();

app.Run();