using AttendanceManager.Components;
using AttendanceManager.Data;
using AttendanceManager.Repository;
using AttendanceManager.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
var builderOptions = new WebApplicationOptions
{
    Args = args,
    WebRootPath = Directory.Exists(webRootPath) ? webRootPath : null
};

var builder = WebApplication.CreateBuilder(builderOptions);
builder.WebHost.UseIISIntegration();

builder.Logging.AddConsole();

var configuredPort =
    Environment.GetEnvironmentVariable("PORT") ??
    builder.Configuration["Server:Port"];

if (!string.IsNullOrWhiteSpace(configuredPort) &&
    int.TryParse(configuredPort, out var port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}

builder.Services.AddSingleton<DbService>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Database")));
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
