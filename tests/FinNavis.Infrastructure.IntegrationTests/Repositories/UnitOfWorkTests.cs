using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Infrastructure.Persistence;
using FinNavis.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinNavis.Infrastructure.IntegrationTests.Repositories;

[Collection(DatabaseCollection.Name)]
public sealed class UnitOfWorkTests : IAsyncLifetime
{
    private readonly PostgresFixture _database;

    public UnitOfWorkTests(PostgresFixture database) => _database = database;

    public Task InitializeAsync() => _database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SaveChangesAsync_WithEveryChangeValid_WritesTheWholeUnit()
    {
        Account account = LedgerData.NewAccount("Cash wallet");
        Category category = LedgerData.NewCategory("Groceries", CategoryKind.Expense);
        Transaction transaction = LedgerData.NewTransaction(account.Id, category.Id, 25m);

        await using (var write = _database.CreateDbContext())
        {
            new AccountRepository(write).Add(account);
            new CategoryRepository(write).Add(category);
            new TransactionRepository(write).Add(transaction);

            await new UnitOfWork(write).SaveChangesAsync(CancellationToken.None);
        }

        await using var read = _database.CreateDbContext();

        (await new AccountRepository(read).GetByIdAsync(account.Id, CancellationToken.None))
            .Should().NotBeNull();
        (await new TransactionRepository(read).GetByIdAsync(transaction.Id, CancellationToken.None))
            .Should().NotBeNull();
    }

    // A commit that fails leaves nothing half-written. The transaction names a category that
    // was never staged, so the foreign key refuses the last statement of the batch — and the
    // account staged before it has to go with it.
    [Fact]
    public async Task SaveChangesAsync_WhenOneChangeIsRefused_RollsBackEveryChangeInTheUnit()
    {
        Account account = LedgerData.NewAccount("Cash wallet");
        Transaction transaction = LedgerData.NewTransaction(account.Id, LedgerData.NewId(), 25m);

        await using (var write = _database.CreateDbContext())
        {
            new AccountRepository(write).Add(account);
            new TransactionRepository(write).Add(transaction);

            Func<Task> commit = () => new UnitOfWork(write).SaveChangesAsync(CancellationToken.None);

            await commit.Should().ThrowAsync<DbUpdateException>();
        }

        await using var read = _database.CreateDbContext();

        (await new AccountRepository(read).GetByIdAsync(account.Id, CancellationToken.None))
            .Should().BeNull();
        (await new TransactionRepository(read).GetByIdAsync(transaction.Id, CancellationToken.None))
            .Should().BeNull();
    }
}
