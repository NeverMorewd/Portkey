  using Portkey.Api.Features.Ports;
using Portkey.Api.Features.Ports.Providers;

var builder = WebApplication.CreateBuilder(args);

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
  var app = builder.Build();

  app.UseCors("AllowAll");
  app.UseDefaultFiles();
  app.UseStaticFiles();
  app.MapFallbackToFile("index.html");
  app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
  app.MapGet("/api/ports", async (PortScannerService scanner) => Results.Ok(await scanner.GetListeningPortsAsync()));

  app.Run();