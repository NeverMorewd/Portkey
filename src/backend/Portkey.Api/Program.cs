using Microsoft.EntityFrameworkCore;
using Portkey.Api.Data;
using Portkey.Api.Features.Ports;
using Portkey.Api.Features.Ports.Providers;
using Portkey.Api.Features.Services;

var builder = WebApplication.CreateBuilder(args);

//builder.Logging.AddConsole();

builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowAll", policy =>
  {
    policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
  });
});

if (OperatingSystem.IsWindows())
  builder.Services.AddSingleton<IPortProvider, WindowsPortProvider>();
else if (OperatingSystem.IsMacOS())
  builder.Services.AddSingleton<IPortProvider, MacOsPortProvider>();
else if (OperatingSystem.IsLinux())
  builder.Services.AddSingleton<IPortProvider, LinuxPortProvider>();
else
  throw new PlatformNotSupportedException("Only Windows and macOS are supported.");

builder.Services.AddSingleton<PortScannerService>();

builder.Services.AddDbContext<PortkeyDbContext>(options =>
     options.UseSqlite("Data Source=portkey.db"));
builder.Services.AddScoped<ServiceManager>();
builder.Services.ConfigureHttpJsonOptions(options =>
  {
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
  });


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<PortkeyDbContext>();
  db.Database.EnsureCreated();
}

app.UseCors("AllowAll");
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/ports", async (PortScannerService scanner) => Results.Ok(await scanner.GetListeningPortsAsync()));
app.MapServiceEndpoints();
app.Run();