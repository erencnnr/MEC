using Microsoft.Extensions.FileProviders; // Bu namespace'i eklemeyi unutmayýn

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 1. ADIM: CORS Politikasýný Tanýmla
// (Farklý porttaki UI projesinin buraya eriþebilmesi için gerekli)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()   // Her yerden gelen isteðe izin ver
                  .AllowAnyMethod()   // GET, POST, PUT vb. hepsine izin ver
                  .AllowAnyHeader();  // Tüm baþlýklara izin ver
        });
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 2. ADIM: CORS'u Aktif Et
app.UseCors("AllowAll");

app.UseHttpsRedirection();

// 3. ADIM: Resim Klasörünü Ayarla ve Dýþarý Aç
string imagePath = @"C:\Images";

// Klasör yoksa oluþtur (DirectoryNotFound hatasýný önler)
if (!Directory.Exists(imagePath))
{
    Directory.CreateDirectory(imagePath);
}

// Klasörü /static adresiyle sun
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imagePath),
    RequestPath = "/static"
});

app.UseAuthorization();

app.MapControllers();

app.Run();
