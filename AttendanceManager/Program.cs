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
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");
app.UseAuthorization();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
