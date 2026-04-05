using Portkey.Api.Data;
using Portkey.Api.Features.Health;

namespace Portkey.Api.Features.Services;

public static class ServiceEndpoints
{
    public static void MapServiceEndpoints(this WebApplication app)
    {
        app.MapGet("/api/services", async (ServiceManager manager) =>
            Results.Ok(await manager.GetServices()));

        app.MapPost("/api/services", async (ServiceEntry entry, ServiceManager manager) =>
            Results.Ok(await manager.AddService(entry)));

        app.MapDelete("/api/services/{id}", async (int id, ServiceManager manager) =>
            Results.Ok(await manager.DeleteService(id)));

        app.MapPut("/api/services/{id}/start", async (int id, ServiceManager manager) =>
            Results.Ok(await manager.StartService(id)));

        app.MapPut("/api/services/{id}/stop", async (int id, ServiceManager manager) =>
            Results.Ok(await manager.StopService(id)));

        app.MapPost("/api/services/{id}/check", async (int id, PortkeyDbContext db, HealthChecker checker) =>
        {
            var svc = await db.ServiceEntries.FindAsync(id);
            if (svc is null) return Results.NotFound();
            if (string.IsNullOrEmpty(svc.Address) || svc.Port == 0)
                return Results.Ok(new { healthy = false, reason = "No address/port configured" });
            var result = await checker.CheckAsync(svc.Address, svc.Port);
            return Results.Ok(new { healthy = result.Healthy, reason = result.Reason, checkedAt = result.CheckedAt });
        });
    }
}