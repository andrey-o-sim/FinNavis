using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;
using FinNavis.Infrastructure.IntegrationTests.Repositories;
using FinNavis.Infrastructure.Persistence;
using FinNavis.Infrastructure.Queries;
using FinNavis.Infrastructure.Repositories;

namespace FinNavis.Infrastructure.IntegrationTests.Queries;

[Collection(DatabaseCollection.Name)]
public sealed class BalanceQueryTests : IAsyncLifetime
{
    private readonly PostgresFixture _database;

    public BalanceQueryTests(PostgresFixture database) => _database = database;

    public Task InitializeAsync() => _database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // Spending lowers the balance, earning raises it, and a transfer moves it between two
    // accounts. 500 earned - 120.25 spent - 80 sent away = 299.75.
    [Fact]
    public async Task Balance_AccountWithExpensesIncomeAndTransfers_EqualsSumOfMovements()
    {
        Account wallet = LedgerData.NewAccount("Cash wallet");
        Account jar = LedgerData.NewAccount("Savings jar");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);
        Category salary = LedgerData.NewCategory("Salary", CategoryKind.Income);

        await StoreAsync(
            [wallet, jar],
            [groceries, salary],
            [
                LedgerData.NewTransaction(wallet.Id, salary.Id, 500m),
                LedgerData.NewTransaction(wallet.Id, groceries.Id, 120.25m),
            ],
            [LedgerData.NewTransfer(wallet.Id, jar.Id, 80m)]);

        IReadOnlyList<AccountBalanceRow> rows = await ReadBalancesAsync(wallet.Id);

        rows.Should().ContainSingle();
        rows[0].AccountId.Should().Be(wallet.Id);
        rows[0].Name.Should().Be("Cash wallet");
        rows[0].Balance.Should().Be(new Money(299.75m));
    }

    // A transfer between the owner's own accounts moves money, it does not create or
    // destroy any. The total over every account is the same before and after.
    [Fact]
    public async Task Balance_TransferBetweenOwnAccounts_TotalUnchanged()
    {
        Account wallet = LedgerData.NewAccount("Cash wallet");
        Account jar = LedgerData.NewAccount("Savings jar");
        Category salary = LedgerData.NewCategory("Salary", CategoryKind.Income);

        await StoreAsync([wallet, jar], [salary], [LedgerData.NewTransaction(wallet.Id, salary.Id, 500m)], []);

        Money totalBeforeTransfer = await ReadTotalAsync();

        await StoreTransferAsync(LedgerData.NewTransfer(wallet.Id, jar.Id, 180m));

        Money totalAfterTransfer = await ReadTotalAsync();

        totalBeforeTransfer.Should().Be(new Money(500m));
        totalAfterTransfer.Should().Be(totalBeforeTransfer);

        IReadOnlyList<AccountBalanceRow> rows = await ReadBalancesAsync(accountId: null);
        rows.Should().HaveCount(2);
        rows.Single(row => row.AccountId == wallet.Id).Balance.Should().Be(new Money(320m));
        rows.Single(row => row.AccountId == jar.Id).Balance.Should().Be(new Money(180m));
    }

    [Fact]
    public async Task Balance_AccountThatRecordedNothing_IsZero()
    {
        Account wallet = LedgerData.NewAccount("Cash wallet");
        await StoreAsync([wallet], [], [], []);

        IReadOnlyList<AccountBalanceRow> rows = await ReadBalancesAsync(wallet.Id);

        rows.Should().ContainSingle().Which.Balance.Should().Be(Money.Zero);
    }

    [Fact]
    public async Task AnyMovementNamesAccountAsync_WithATransactionOnTheAccount_IsTrue()
    {
        Account wallet = LedgerData.NewAccount("Cash wallet");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);

        await StoreAsync([wallet], [groceries], [LedgerData.NewTransaction(wallet.Id, groceries.Id, 10m)], []);

        (await AnyMovementNamesAccountAsync(wallet.Id)).Should().BeTrue();
    }

    // A transfer names an account from either side, so either side has to refuse the delete.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AnyMovementNamesAccountAsync_WithOnlyATransferOnOneSide_IsTrue(bool asSource)
    {
        Account wallet = LedgerData.NewAccount("Cash wallet");
        Account jar = LedgerData.NewAccount("Savings jar");

        Transfer transfer = asSource
            ? LedgerData.NewTransfer(wallet.Id, jar.Id, 25m)
            : LedgerData.NewTransfer(jar.Id, wallet.Id, 25m);

        await StoreAsync([wallet, jar], [], [], [transfer]);

        (await AnyMovementNamesAccountAsync(wallet.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task AnyMovementNamesAccountAsync_WithNoMovementNamingTheAccount_IsFalse()
    {
        Account wallet = LedgerData.NewAccount("Cash wallet");
        Account untouched = LedgerData.NewAccount("Spare card");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);

        await StoreAsync(
            [wallet, untouched],
            [groceries],
            [LedgerData.NewTransaction(wallet.Id, groceries.Id, 10m)],
            []);

        (await AnyMovementNamesAccountAsync(untouched.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task AnyTransactionNamesCategoryAsync_WithATransactionFiledUnderIt_IsTrue()
    {
        Account wallet = LedgerData.NewAccount("Cash wallet");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);

        await StoreAsync([wallet], [groceries], [LedgerData.NewTransaction(wallet.Id, groceries.Id, 10m)], []);

        (await AnyTransactionNamesCategoryAsync(groceries.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task AnyTransactionNamesCategoryAsync_WithNoTransactionFiledUnderIt_IsFalse()
    {
        Account wallet = LedgerData.NewAccount("Cash wallet");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);
        Category unused = LedgerData.NewCategory("Books", CategoryKind.Expense);

        await StoreAsync(
            [wallet],
            [groceries, unused],
            [LedgerData.NewTransaction(wallet.Id, groceries.Id, 10m)],
            []);

        (await AnyTransactionNamesCategoryAsync(unused.Id)).Should().BeFalse();
    }

    // A transfer carries no category, so a category a transfer "touched" does not exist — the
    // closest case is a category no transaction names while transfers are recorded.
    [Fact]
    public async Task AnyTransactionNamesCategoryAsync_WithOnlyTransfersRecorded_IsFalse()
    {
        Account wallet = LedgerData.NewAccount("Cash wallet");
        Account jar = LedgerData.NewAccount("Savings jar");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);

        await StoreAsync([wallet, jar], [groceries], [], [LedgerData.NewTransfer(wallet.Id, jar.Id, 30m)]);

        (await AnyTransactionNamesCategoryAsync(groceries.Id)).Should().BeFalse();
    }

    private async Task<Money> ReadTotalAsync()
    {
        IReadOnlyList<AccountBalanceRow> rows = await ReadBalancesAsync(accountId: null);

        return rows.Aggregate(Money.Zero, (total, row) => total + row.Balance);
    }

    private async Task<IReadOnlyList<AccountBalanceRow>> ReadBalancesAsync(Guid? accountId)
    {
        await using var context = _database.CreateDbContext();

        return await new LedgerBalanceQueries(context).GetAccountBalancesAsync(accountId, CancellationToken.None);
    }

    private async Task<bool> AnyMovementNamesAccountAsync(Guid accountId)
    {
        await using var context = _database.CreateDbContext();

        return await new LedgerBalanceQueries(context)
            .AnyMovementNamesAccountAsync(accountId, CancellationToken.None);
    }

    private async Task<bool> AnyTransactionNamesCategoryAsync(Guid categoryId)
    {
        await using var context = _database.CreateDbContext();

        return await new LedgerBalanceQueries(context)
            .AnyTransactionNamesCategoryAsync(categoryId, CancellationToken.None);
    }

    private async Task StoreAsync(
        IReadOnlyList<Account> accounts,
        IReadOnlyList<Category> categories,
        IReadOnlyList<Transaction> transactions,
        IReadOnlyList<Transfer> transfers)
    {
        await using var context = _database.CreateDbContext();

        var accountRepository = new AccountRepository(context);
        foreach (Account account in accounts)
        {
            accountRepository.Add(account);
        }

        var categoryRepository = new CategoryRepository(context);
        foreach (Category category in categories)
        {
            categoryRepository.Add(category);
        }

        var transactionRepository = new TransactionRepository(context);
        foreach (Transaction transaction in transactions)
        {
            transactionRepository.Add(transaction);
        }

        var transferRepository = new TransferRepository(context);
        foreach (Transfer transfer in transfers)
        {
            transferRepository.Add(transfer);
        }

        await new UnitOfWork(context).SaveChangesAsync(CancellationToken.None);
    }

    private async Task StoreTransferAsync(Transfer transfer)
    {
        await using var context = _database.CreateDbContext();

        new TransferRepository(context).Add(transfer);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken.None);
    }
}
