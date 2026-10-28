using FinNavis.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinNavis.Infrastructure.Persistence;

/// <summary>
/// The EF Core session over the ledger's five tables (sad.md §5, <c>data-model.md</c>).
/// </summary>
/// <remarks>
/// One <c>DbSet</c> per aggregate root, and nothing else: there is no rollup table and no cache,
/// because every read is computed on demand (ADR-0002).
/// <para>
/// <b>The database is dumb storage.</b> Every mapping here is structural — a column type, a bound,
/// an index, a foreign key. No business rule is expressed as a <c>CHECK</c>, a trigger or a
/// cascade; the Domain entities refuse first (<c>.claude/rules/migrations.md</c>,
/// <c>data-model.md</c> decisions 6 and 8).
/// </para>
/// </remarks>
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
