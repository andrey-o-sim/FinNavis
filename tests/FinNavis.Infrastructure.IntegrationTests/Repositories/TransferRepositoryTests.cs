using FinNavis.Domain.Entities;
using FinNavis.Domain.ValueObjects;
using FinNavis.Infrastructure.Persistence;
using FinNavis.Infrastructure.Repositories;

namespace FinNavis.Infrastructure.IntegrationTests.Repositories;

[Collection(DatabaseCollection.Name)]
public sealed class TransferRepositoryTests : IAsyncLifetime
{
    private readonly PostgresFixture _database;

    public TransferRepositoryTests(PostgresFixture database) => _database = database;

    public Task InitializeAsync() => _database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Add_AfterTheUnitOfWorkCommits_ReadsBothSidesBackFromOneRow()
    {
        Account source = LedgerData.NewAccount("Cash wallet");
        Account destination = LedgerData.NewAccount("Savings jar");
        Transfer transfer = LedgerData.NewTransfer(source.Id, destination.Id, 250.5000m);

        await StoreAsync(source, destination, transfer);

        await using var read = _database.CreateDbContext();
        Transfer? stored = await new TransferRepository(read)
            .GetByIdAsync(transfer.Id, CancellationToken.None);

        stored.Should().NotBeNull();
        stored!.SourceAccountId.Should().Be(source.Id);
        stored.DestinationAccountId.Should().Be(destination.Id);
        stored.Amount.Should().Be(new Money(250.5m));
        stored.OccurredOn.Should().Be(LedgerData.OccurredOn);
    }

    [Fact]
    public async Task Remove_AfterTheUnitOfWorkCommits_LeavesNothingToLoad()
    {
        Account source = LedgerData.NewAccount("Cash wallet");
        Account destination = LedgerData.NewAccount("Savings jar");
        Transfer transfer = LedgerData.NewTransfer(source.Id, destination.Id, 40m);

        await StoreAsync(source, destination, transfer);

        await using (var write = _database.CreateDbContext())
        {
            var repository = new TransferRepository(write);
            Transfer loaded = (await repository.GetByIdAsync(transfer.Id, CancellationToken.None))!;

            repository.Remove(loaded);
            await new UnitOfWork(write).SaveChangesAsync(CancellationToken.None);
        }

        await using var read = _database.CreateDbContext();
        Transfer? stored = await new TransferRepository(read)
            .GetByIdAsync(transfer.Id, CancellationToken.None);

        stored.Should().BeNull();
    }

    private async Task StoreAsync(Account source, Account destination, Transfer transfer)
    {
        await using var context = _database.CreateDbContext();

        var accounts = new AccountRepository(context);
        accounts.Add(source);
        accounts.Add(destination);
        new TransferRepository(context).Add(transfer);

        await new UnitOfWork(context).SaveChangesAsync(CancellationToken.None);
    }
}
