using FinNavis.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinNavis.Infrastructure.Persistence;

public sealed class FinNavisDbContext : DbContext
{
    public FinNavisDbContext(DbContextOptions<FinNavisDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssemblyMarker).Assembly);
    }
}
