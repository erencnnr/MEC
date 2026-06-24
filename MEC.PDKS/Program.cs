using MEC.PDKS.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

var environment = builder.Configuration["AppSettings:Environment"];
var connectionString = EnsureMySqlConnectionString(
    builder.Configuration.GetConnectionString(environment == "Test" ? "DefaultConnection" : "ProdConnection"));

builder.Services.AddDbContext<PdksDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static string EnsureMySqlConnectionString(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Database connection string is not configured.");
    }

    var builder = new MySqlConnectionStringBuilder(connectionString)
    {
        ConvertZeroDateTime = true
    };

    return builder.ConnectionString;
}
