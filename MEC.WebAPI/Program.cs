var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- 1. CORS Servisini Ekle ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUI",
        policy =>
        {
            // Güvenlik için sadece kendi UI adresinize izin verebilirsiniz
            // veya geliþtirme ortamý için AllowAnyOrigin kullanabilirsiniz.
            policy.WithOrigins("https://localhost:7255") // UI projenizin adresi
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// --- 2. CORS Middleware'ini Kullan ---
app.UseCors("AllowUI");

app.UseAuthorization();

app.MapControllers();

app.Run();