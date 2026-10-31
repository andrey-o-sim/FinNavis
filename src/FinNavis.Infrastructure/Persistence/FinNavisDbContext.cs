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

    public DbSet<MappingProfile> MappingProfiles => Set<MappingProfile>();

    public DbSet<MappingRule> MappingRules => Set<MappingRule>();

    public DbSet<ImportRun> ImportRuns => Set<ImportRun>();

    public DbSet<ImportTransactionLink> ImportTransactionLinks => Set<ImportTransactionLink>();

    public DbSet<ImportTransferLink> ImportTransferLinks => Set<ImportTransferLink>();

    public DbSet<ImportCreatedAccountLink> ImportCreatedAccountLinks => Set<ImportCreatedAccountLink>();

    public DbSet<ImportCreatedCategoryLink> ImportCreatedCategoryLinks => Set<ImportCreatedCategoryLink>();

    public DbSet<SetAsideRow> SetAsideRows => Set<SetAsideRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinNavisDbContext).Assembly);
    }
}
