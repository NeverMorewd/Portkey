using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Portkey.Api.Data;
using Portkey.Api.Features.Services;
using Portkey.Api.Hubs;

namespace Portkey.Api.Features.Health;

public class HealthMonitorService(
    IServiceScopeFactory scopeFactory,
    IHubContext<LogHub> hub,
    HealthChecker checker,
    ILogger<HealthMonitorService> logger) : BackgroundService
{
    private readonly Dictionary<int, int> _failureCounts = new();
    private const int FailureThreshold = 3;
    private const int IntervalSeconds = 30;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Wait for services to start before first check
        await Task.Delay(TimeSpan.FromSeconds(15), ct);

        while (!ct.IsCancellationRequested)
        {
            try { await RunChecksAsync(ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Health check cycle failed");
            }
            await Task.Delay(TimeSpan.FromSeconds(IntervalSeconds), ct);
        }
    }

    public async Task RunChecksAsync(CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PortkeyDbContext>();

        var services = await db.ServiceEntries
            .Where(s => (s.Status == ServiceStatus.Running || s.Status == ServiceStatus.Unhealthy)
                        && s.Address != "" && s.Port > 0)
            .ToListAsync(ct);

        foreach (var svc in services)
        {
            var result = await checker.CheckAsync(svc.Address, svc.Port, ct: ct);
            await ApplyResultAsync(svc, result, db, ct);
        }
    }

    private async Task ApplyResultAsync(
        ServiceEntry svc, HealthCheckResult result, PortkeyDbContext db, CancellationToken ct)
    {
        if (result.Healthy)
        {
            _failureCounts[svc.Id] = 0;

            if (svc.Status == ServiceStatus.Unhealthy)
            {
                svc.Status = ServiceStatus.Running;
                db.Update(svc);
                await db.SaveChangesAsync(ct);
                await hub.Clients.All.SendAsync(
                    "ServiceStatusChanged", new { id = svc.Id, status = "Running" }, ct);
                logger.LogInformation("Service {Id} ({Name}) recovered", svc.Id, svc.Name);
            }
        }
        else
        {
            var count = _failureCounts.GetValueOrDefault(svc.Id) + 1;
            _failureCounts[svc.Id] = count;
            logger.LogDebug("Service {Id} check failed ({Count}/{Threshold}): {Reason}",
                svc.Id, count, FailureThreshold, result.Reason);

            if (count >= FailureThreshold && svc.Status == ServiceStatus.Running)
            {
                svc.Status = ServiceStatus.Unhealthy;
                db.Update(svc);
                await db.SaveChangesAsync(ct);
                await hub.Clients.All.SendAsync(
                    "ServiceStatusChanged", new { id = svc.Id, status = "Unhealthy" }, ct);
                logger.LogWarning("Service {Id} ({Name}) marked Unhealthy: {Reason}",
                    svc.Id, svc.Name, result.Reason);
            }
        }
    }
}
