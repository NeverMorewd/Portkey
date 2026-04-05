using Microsoft.EntityFrameworkCore;
using Portkey.Api.Data;

namespace Portkey.Api.Features.Env;

public static class EnvEndpoints
{
    public static void MapEnvEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/env");

        // ── Projects ──────────────────────────────────────────────────────────

        group.MapGet("/projects", async (PortkeyDbContext db) =>
            Results.Ok(await db.EnvProjects.ToListAsync()));

        group.MapPost("/projects", async (EnvProject project, PortkeyDbContext db) =>
        {
            if (!Directory.Exists(project.Path))
                return Results.BadRequest("Directory does not exist.");
            db.EnvProjects.Add(project);
            await db.SaveChangesAsync();
            return Results.Ok(project);
        });

        group.MapDelete("/projects/{id}", async (int id, PortkeyDbContext db) =>
        {
            var project = await db.EnvProjects.FindAsync(id);
            if (project is null) return Results.NotFound();
            db.EnvProjects.Remove(project);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // ── Files ─────────────────────────────────────────────────────────────

        group.MapGet("/projects/{id}/files", async (int id, PortkeyDbContext db, EnvService svc) =>
        {
            var project = await db.EnvProjects.FindAsync(id);
            if (project is null) return Results.NotFound();
            return Results.Ok(svc.ListEnvFiles(project.Path));
        });

        group.MapGet("/projects/{id}/file", async (int id, string name, PortkeyDbContext db, EnvService svc) =>
        {
            if (!EnvService.IsValidEnvFilename(name))
                return Results.BadRequest("Invalid filename.");

            var project = await db.EnvProjects.FindAsync(id);
            if (project is null) return Results.NotFound();

            var filePath = Path.Combine(project.Path, name);
            var content = File.Exists(filePath) ? await File.ReadAllTextAsync(filePath) : "";
            var sensitiveKeys = svc.LoadSensitiveKeys(project.Path);
            var entries = svc.ParseFile(content, sensitiveKeys);
            return Results.Ok(entries);
        });

        group.MapPut("/projects/{id}/file", async (int id, string name,
            List<EnvEntry> entries, PortkeyDbContext db, EnvService svc) =>
        {
            if (!EnvService.IsValidEnvFilename(name))
                return Results.BadRequest("Invalid filename.");

            var project = await db.EnvProjects.FindAsync(id);
            if (project is null) return Results.NotFound();

            var filePath = Path.Combine(project.Path, name);
            var content = svc.SerializeFile(entries);
            await File.WriteAllTextAsync(filePath, content);

            var sensitiveKeys = entries.Where(e => e.IsSensitive).Select(e => e.Key);
            svc.SaveSensitiveKeys(project.Path, sensitiveKeys);

            return Results.NoContent();
        });
    }
}
