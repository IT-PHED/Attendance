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
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

var appWebRoot = app.Environment.WebRootPath;
var defaultIndexPath = appWebRoot is null ? null : Path.Combine(appWebRoot, "index.html");
// app.Logger.LogInformation("Starting AttendanceManager in {Environment}", app.Environment.EnvironmentName);
// app.Logger.LogInformation("ContentRootPath={ContentRootPath}, WebRootPath={WebRootPath}, WebRootExists={WebRootExists}", app.Environment.ContentRootPath, appWebRoot, appWebRoot != null && Directory.Exists(appWebRoot));
// app.Logger.LogInformation("Default index.html path={IndexPath}, Exists={IndexExists}", defaultIndexPath, defaultIndexPath != null && File.Exists(defaultIndexPath));
// app.Logger.LogInformation("Configured URLs: {Urls}", string.Join(',', app.Urls));
// app.Logger.LogInformation("Server port env=PORT:{PortEnv}, config=Server:Port:{PortConfig}", Environment.GetEnvironmentVariable("PORT"), builder.Configuration["Server:Port"]);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseRouting();

app.UseAuthorization();

app.MapControllers(); // MUST come before fallback

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapFallbackToFile("index.html");

app.Run();
