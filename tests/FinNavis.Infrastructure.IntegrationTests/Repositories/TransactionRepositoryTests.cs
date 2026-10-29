using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;
using FinNavis.Infrastructure.Persistence;
using FinNavis.Infrastructure.Repositories;

namespace FinNavis.Infrastructure.IntegrationTests.Repositories;

[Collection(DatabaseCollection.Name)]
public sealed class TransactionRepositoryTests : IAsyncLifetime
{
    private readonly PostgresFixture _database;

    public TransactionRepositoryTests(PostgresFixture database) => _database = database;

    public Task InitializeAsync() => _database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Add_AfterTheUnitOfWorkCommits_ReadsTheTransactionBackFromPostgres()
    {
        Account account = LedgerData.NewAccount("Cash wallet");
        Category category = LedgerData.NewCategory("Groceries", CategoryKind.Expense);
        Transaction transaction = LedgerData.NewTransaction(account.Id, category.Id, 12.3456m, "Bread");

        await StoreAsync(account, category, transaction);

        await using var read = _database.CreateDbContext();
        Transaction? stored = await new TransactionRepository(read)
            .GetByIdAsync(transaction.Id, CancellationToken.None);

        stored.Should().NotBeNull();
        stored!.AccountId.Should().Be(account.Id);
        stored.CategoryId.Should().Be(category.Id);
        stored.OccurredOn.Should().Be(LedgerData.OccurredOn);
        stored.Note.Should().Be("Bread");

        // numeric(19,4): the fourth decimal place has to survive the round trip (CLAUDE.md).
        stored.Amount.Should().Be(new Money(12.3456m));
    }

    [Fact]
    public async Task Remove_AfterTheUnitOfWorkCommits_LeavesNothingToLoad()
    {
        Account account = LedgerData.NewAccount("Cash wallet");
        Category category = LedgerData.NewCategory("Groceries", CategoryKind.Expense);
        Transaction transaction = LedgerData.NewTransaction(account.Id, category.Id, 10m);

        await StoreAsync(account, category, transaction);

        await using (var write = _database.CreateDbContext())
        {
            var repository = new TransactionRepository(write);
            Transaction loaded = (await repository.GetByIdAsync(transaction.Id, CancellationToken.None))!;

            repository.Remove(loaded);
            await new UnitOfWork(write).SaveChangesAsync(CancellationToken.None);
        }

        await using var read = _database.CreateDbContext();
        Transaction? stored = await new TransactionRepository(read)
            .GetByIdAsync(transaction.Id, CancellationToken.None);

        stored.Should().BeNull();
    }

    private async Task StoreAsync(Account account, Category category, Transaction transaction)
    {
        await using var context = _database.CreateDbContext();

        new AccountRepository(context).Add(account);
        new CategoryRepository(context).Add(category);
        new TransactionRepository(context).Add(transaction);

        await new UnitOfWork(context).SaveChangesAsync(CancellationToken.None);
    }
}
