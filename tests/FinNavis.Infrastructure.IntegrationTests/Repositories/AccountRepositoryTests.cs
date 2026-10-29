using FinNavis.Domain.Entities;
using FinNavis.Infrastructure.Persistence;
using FinNavis.Infrastructure.Repositories;

namespace FinNavis.Infrastructure.IntegrationTests.Repositories;

[Collection(DatabaseCollection.Name)]
public sealed class AccountRepositoryTests : IAsyncLifetime
{
    private readonly PostgresFixture _database;

    public AccountRepositoryTests(PostgresFixture database) => _database = database;

    public Task InitializeAsync() => _database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Add_AfterTheUnitOfWorkCommits_ReadsTheAccountBackFromPostgres()
    {
        Account account = LedgerData.NewAccount("Cash wallet");

        await using (var write = _database.CreateDbContext())
        {
            new AccountRepository(write).Add(account);
            await new UnitOfWork(write).SaveChangesAsync(CancellationToken.None);
        }

        await using var read = _database.CreateDbContext();
        Account? stored = await new AccountRepository(read).GetByIdAsync(account.Id, CancellationToken.None);

        stored.Should().NotBeNull();
        stored!.Id.Should().Be(account.Id);
        stored.Name.Should().Be("Cash wallet");
    }

    [Fact]
    public async Task Remove_AfterTheUnitOfWorkCommits_LeavesNothingToLoad()
    {
        Account account = LedgerData.NewAccount("Old card");
        await StoreAsync(account);

        await using (var write = _database.CreateDbContext())
        {
            var repository = new AccountRepository(write);
            Account loaded = (await repository.GetByIdAsync(account.Id, CancellationToken.None))!;

            repository.Remove(loaded);
            await new UnitOfWork(write).SaveChangesAsync(CancellationToken.None);
        }

        await using var read = _database.CreateDbContext();
        Account? stored = await new AccountRepository(read).GetByIdAsync(account.Id, CancellationToken.None);

        stored.Should().BeNull();
    }

    [Fact]
    public async Task ExistsWithNameAsync_WithTheNameAnAccountAlreadyCarries_IsTrue()
    {
        await StoreAsync(LedgerData.NewAccount("Savings jar"));

        await using var read = _database.CreateDbContext();
        bool exists = await new AccountRepository(read)
            .ExistsWithNameAsync("Savings jar", CancellationToken.None);

        exists.Should().BeTrue();
    }

    // ux_accounts_name is scoped to the name alone, so the check answers on the name alone.
    [Fact]
    public async Task ExistsWithNameAsync_WithANameNoAccountCarries_IsFalse()
    {
        await StoreAsync(LedgerData.NewAccount("Savings jar"));

        await using var read = _database.CreateDbContext();
        bool exists = await new AccountRepository(read)
            .ExistsWithNameAsync("Spending jar", CancellationToken.None);

        exists.Should().BeFalse();
    }

    private async Task StoreAsync(Account account)
    {
        await using var context = _database.CreateDbContext();

        new AccountRepository(context).Add(account);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken.None);
    }
}
