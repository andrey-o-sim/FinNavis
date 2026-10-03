using FinNavis.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinNavis.Infrastructure.Persistence;

public sealed class FinNavisDbContext(DbContextOptions<FinNavisDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Picks up every IEntityTypeConfiguration in this assembly, so a new entity only
        // needs its configuration file. Nothing has to be registered here.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssemblyMarker).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
