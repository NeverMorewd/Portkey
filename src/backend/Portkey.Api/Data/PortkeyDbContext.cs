  using Microsoft.EntityFrameworkCore;
  using Portkey.Api.Features.Services;

  namespace Portkey.Api.Data;

  public class PortkeyDbContext : DbContext
  {
      public PortkeyDbContext(DbContextOptions<PortkeyDbContext> options) : base(options) { }

      public DbSet<ServiceEntry> ServiceEntries => Set<ServiceEntry>();
  }
