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
    }
}