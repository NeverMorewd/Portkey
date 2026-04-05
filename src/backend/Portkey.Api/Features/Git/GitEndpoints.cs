using Microsoft.EntityFrameworkCore;
using Portkey.Api.Data;

namespace Portkey.Api.Features.Git;

public static class GitEndpoints
{
    public static void MapGitEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/git");

        // ── Scan roots ────────────────────────────────────────────────────────

        group.MapGet("/roots", async (PortkeyDbContext db) =>
            Results.Ok(await db.GitRoots.ToListAsync()));

        group.MapPost("/roots", async (GitRoot root, PortkeyDbContext db) =>
        {
            if (!Directory.Exists(root.Path))
                return Results.BadRequest("Directory does not exist.");
            db.GitRoots.Add(root);
            await db.SaveChangesAsync();
            return Results.Ok(root);
        });

        group.MapDelete("/roots/{id}", async (int id, PortkeyDbContext db) =>
        {
            var root = await db.GitRoots.FindAsync(id);
            if (root is null) return Results.NotFound();
            db.GitRoots.Remove(root);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // ── Repo scan ─────────────────────────────────────────────────────────

        group.MapGet("/repos", async (PortkeyDbContext db, GitScanService scanner) =>
        {
            var roots = await db.GitRoots.ToListAsync();
            var repos = roots
                .SelectMany(r => scanner.ScanDirectory(r.Path))
                .OrderBy(r => r.Name)
                .ToList();
            return Results.Ok(repos);
        });

        group.MapGet("/repos/refresh", async (string path, GitScanService scanner) =>
        {
            if (!Directory.Exists(path))
                return Results.BadRequest("Path does not exist.");
            try { return Results.Ok(scanner.GetRepoInfo(path)); }
            catch (Exception ex) { return Results.BadRequest(ex.Message); }
        });
    }
}
