using Microsoft.EntityFrameworkCore;
using Portkey.Api.Features.Env;
using Portkey.Api.Features.Git;
using Portkey.Api.Features.Services;

namespace Portkey.Api.Data;

public class PortkeyDbContext : DbContext
{
    public PortkeyDbContext(DbContextOptions<PortkeyDbContext> options) : base(options) { }

    public DbSet<ServiceEntry> ServiceEntries => Set<ServiceEntry>();
    public DbSet<EnvProject> EnvProjects => Set<EnvProject>();
    public DbSet<GitRoot> GitRoots => Set<GitRoot>();
}
