using MEC.Portal.Controllers;
using MEC.Portal.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Text.RegularExpressions;

static class PortalUserFormChecks
{
    public static async Task RunAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(AccountController).Assembly.GetName().Name,
            ContentRootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../MEC.Portal")),
            EnvironmentName = "Development"
        });
        builder.Logging.ClearProviders();
        builder.Services.AddControllersWithViews();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        await using var app = builder.Build();
        app.MapControllers();
        // Initialize endpoint routing for the real MVC form-action tag helper.
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();
        using var scope = app.Services.CreateScope();
        var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        http.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(), "Form rendering test"));
        http.Request.Scheme = "http";
        http.Request.Host = new HostString("localhost");
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var engine = http.RequestServices.GetRequiredService<IRazorViewEngine>();
        var view = engine.GetView(null, "/Views/Admin/PortalUserDetail.cshtml", true);
        if (!view.Success) throw new InvalidOperationException("Portal user view could not be rendered.");
        var metadata = http.RequestServices.GetRequiredService<IModelMetadataProvider>();
        var model = new AdminPortalUserEditViewModel
        {
            Id = 1, FirstName = "Test", LastName = "User", Email = "test@example.invalid",
            PhoneNumber = "05551234567", Title = "", LocationIds = new() { 2 },
            LocationOptions = new() { new("School 1", "1"), new("School 2", "2") }
        };
        using var writer = new StringWriter();
        await view.View.RenderAsync(new ViewContext(action, view.View,
            new ViewDataDictionary(metadata, action.ModelState) { Model = model },
            new TempDataDictionary(http, http.RequestServices.GetRequiredService<ITempDataProvider>()),
            writer, new HtmlHelperOptions()));
        var html = writer.ToString();
        var locationButton = Regex.Matches(html, "<button\\b[^>]*>").Select(x => x.Value)
            .Single(x => x.Contains("formaction="));
        if (!locationButton.Contains("formaction=\"/Admin/PortalUsers/1/Locations\"") || !locationButton.Contains("formnovalidate"))
            throw new InvalidOperationException("Location save must use its own endpoint without validating unrelated profile fields: " + locationButton);
        foreach (var (name, value) in new[] { ("Email", model.Email), ("PhoneNumber", model.PhoneNumber) })
        {
            var input = Regex.Matches(html, "<input\\b[^>]*>").Select(x => x.Value)
                .Single(x => x.Contains($"name=\"{name}\""));
            if (!input.Contains($"value=\"{value}\""))
                throw new InvalidOperationException($"Existing {name} must populate the edit form: {input}");
        }
        if (metadata.GetMetadataForProperty(typeof(AdminPortalUserEditViewModel), "Title").IsRequired)
            throw new InvalidOperationException("An empty optional title must not block location edits.");
        Console.WriteLine("PASS: Portal user form preserves contact values and allows an empty title.");
        await app.StopAsync();
    }
}
