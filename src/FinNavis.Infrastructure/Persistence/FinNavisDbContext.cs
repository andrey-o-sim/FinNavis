using FinNavis.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinNavis.Infrastructure.Persistence;

public sealed class FinNavisDbContext(DbContextOptions<FinNavisDbContext> options)
    : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<CategoryGroup> CategoryGroups => Set<CategoryGroup>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<Transfer> Transfers => Set<Transfer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinNavisDbContext).Assembly);
    }
}
