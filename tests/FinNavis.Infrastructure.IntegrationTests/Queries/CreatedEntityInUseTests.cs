using System.Globalization;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;
using FinNavis.Infrastructure.IntegrationTests.Repositories;
using FinNavis.Infrastructure.Persistence;
using FinNavis.Infrastructure.Queries;
using FinNavis.Infrastructure.Repositories;
using Npgsql;

namespace FinNavis.Infrastructure.IntegrationTests.Queries;

[Collection(DatabaseCollection.Name)]
public sealed class CreatedEntityInUseTests : IAsyncLifetime
{
    private static readonly DateTimeOffset HandedOverAt = new(2026, 4, 2, 9, 0, 0, TimeSpan.Zero);

    private readonly PostgresFixture _database;

    public CreatedEntityInUseTests(PostgresFixture database) => _database = database;

    public Task InitializeAsync() => _database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // After the load the owner typed a purchase by hand on the account the load created. Undo
    // must keep that account, or the hand-typed purchase loses the account it belongs to.
    [Fact]
    public async Task GetCreationsNamedOutsideRunAsync_CreatedAccountNamedByHandTypedTransaction_ReportsItInUse()
    {
        Account createdByRun = LedgerData.NewAccount("Imported card");
        Account typedByOwner = LedgerData.NewAccount("Cash wallet");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);

        Transaction recordedByRun = Movement(createdByRun.Id, groceries.Id, 30m, "2026-03-05");
        Transaction typedOnCreatedAccount = Movement(createdByRun.Id, groceries.Id, 12m, "2026-04-03");
        Transaction typedOnOwnAccount = Movement(typedByOwner.Id, groceries.Id, 8m, "2026-04-03");

        await StoreAsync(
            [createdByRun, typedByOwner],
            [groceries],
            [recordedByRun, typedOnCreatedAccount, typedOnOwnAccount],
            []);
        ImportRun run = await StoreRunAsync([createdByRun], [], [recordedByRun], []);

        ImportRunCreationsInUse inUse = await CheckAsync(run.Id);

        // The owner's own account is named outside the run too, but the run did not create it,
        // so it is not this run's to report.
        inUse.AccountIds.Should().Equal(createdByRun.Id);
        inUse.CategoryIds.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCreationsNamedOutsideRunAsync_CreatedAccountNamedOnlyByTheRunsOwnMovements_ReportsItNotInUse()
    {
        Account createdByRun = LedgerData.NewAccount("Imported card");
        Account existing = LedgerData.NewAccount("Savings jar");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);

        Transaction recordedByRun = Movement(createdByRun.Id, groceries.Id, 30m, "2026-03-05");
        Transfer movedByRun = Move(createdByRun.Id, existing.Id, 100m, "2026-03-06");

        await StoreAsync([createdByRun, existing], [groceries], [recordedByRun], [movedByRun]);
        ImportRun run = await StoreRunAsync([createdByRun], [], [recordedByRun], [movedByRun]);

        ImportRunCreationsInUse inUse = await CheckAsync(run.Id);

        inUse.AccountIds.Should().BeEmpty();
        inUse.CategoryIds.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCreationsNamedOutsideRunAsync_CreatedCategoryNamedByHandTypedTransaction_ReportsItInUse()
    {
        Account card = LedgerData.NewAccount("Debit card");
        Category createdByRun = LedgerData.NewCategory("Pharmacy", CategoryKind.Expense);
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);

        Transaction recordedByRun = Movement(card.Id, createdByRun.Id, 30m, "2026-03-05");
        Transaction typedWithCreatedCategory = Movement(card.Id, createdByRun.Id, 15m, "2026-04-03");
        Transaction typedWithOwnCategory = Movement(card.Id, groceries.Id, 9m, "2026-04-03");

        await StoreAsync(
            [card],
            [createdByRun, groceries],
            [recordedByRun, typedWithCreatedCategory, typedWithOwnCategory],
            []);
        ImportRun run = await StoreRunAsync([], [createdByRun], [recordedByRun], []);

        ImportRunCreationsInUse inUse = await CheckAsync(run.Id);

        inUse.CategoryIds.Should().Equal(createdByRun.Id);
        inUse.AccountIds.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCreationsNamedOutsideRunAsync_CreatedCategoryNamedOnlyByTheRunsOwnTransactions_ReportsItNotInUse()
    {
        Account card = LedgerData.NewAccount("Debit card");
        Category createdByRun = LedgerData.NewCategory("Pharmacy", CategoryKind.Expense);

        Transaction recordedByRun = Movement(card.Id, createdByRun.Id, 30m, "2026-03-05");

        await StoreAsync([card], [createdByRun], [recordedByRun], []);
        ImportRun run = await StoreRunAsync([], [createdByRun], [recordedByRun], []);

        ImportRunCreationsInUse inUse = await CheckAsync(run.Id);

        inUse.CategoryIds.Should().BeEmpty();
        inUse.AccountIds.Should().BeEmpty();
    }

    // A transfer names two accounts. Missing either side means undo deletes an account a real
    // transfer still points at, and the database refuses that delete in the middle of the undo.
    [Fact]
    public async Task GetCreationsNamedOutsideRunAsync_HandTypedTransferOutOfCreatedAccount_ReportsItInUse()
    {
        Account createdByRun = LedgerData.NewAccount("Imported card");
        Account jar = LedgerData.NewAccount("Savings jar");

        Transfer typedByOwner = Move(createdByRun.Id, jar.Id, 100m, "2026-04-03");

        await StoreAsync([createdByRun, jar], [], [], [typedByOwner]);
        ImportRun run = await StoreRunAsync([createdByRun], [], [], []);

        ImportRunCreationsInUse inUse = await CheckAsync(run.Id);

        inUse.AccountIds.Should().Equal(createdByRun.Id);
    }

    [Fact]
    public async Task GetCreationsNamedOutsideRunAsync_HandTypedTransferIntoCreatedAccount_ReportsItInUse()
    {
        Account createdByRun = LedgerData.NewAccount("Imported card");
        Account wallet = LedgerData.NewAccount("Cash wallet");

        Transfer typedByOwner = Move(wallet.Id, createdByRun.Id, 50m, "2026-04-03");

        await StoreAsync([createdByRun, wallet], [], [], [typedByOwner]);
        ImportRun run = await StoreRunAsync([createdByRun], [], [], []);

        ImportRunCreationsInUse inUse = await CheckAsync(run.Id);

        inUse.AccountIds.Should().Equal(createdByRun.Id);
    }

    // A later load that records movements on the account an earlier load created is outside the
    // earlier run too. Undoing the earlier load must not take the account from under the later one.
    [Fact]
    public async Task GetCreationsNamedOutsideRunAsync_CreatedEntitiesNamedByAnotherRunsMovements_ReportsThemInUse()
    {
        Account createdByRun = LedgerData.NewAccount("Imported card");
        Category createdCategory = LedgerData.NewCategory("Pharmacy", CategoryKind.Expense);

        Transaction recordedByRun = Movement(createdByRun.Id, createdCategory.Id, 30m, "2026-03-05");
        Transaction recordedByLaterRun = Movement(createdByRun.Id, createdCategory.Id, 22m, "2026-04-05");

        await StoreAsync([createdByRun], [createdCategory], [recordedByRun, recordedByLaterRun], []);
        ImportRun run = await StoreRunAsync([createdByRun], [createdCategory], [recordedByRun], []);
        await StoreRunAsync([], [], [recordedByLaterRun], []);

        ImportRunCreationsInUse inUse = await CheckAsync(run.Id);

        inUse.AccountIds.Should().Equal(createdByRun.Id);
        inUse.CategoryIds.Should().Equal(createdCategory.Id);
    }

    // The check is an existence test per created entity, served by the ledger's own foreign key
    // indexes. Every command the check issues is planned, so the check may use one statement or
    // several.
    [Fact]
    public async Task GetCreationsNamedOutsideRunAsync_RunWithCreatedAccountAndCategory_IsPlannedOnTheLedgerForeignKeyIndexes()
    {
        IReadOnlyList<RecordedCommand> check = await RecordCheckAsync();

        await using var connection = await _database.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        List<string> plans = [];
        foreach (RecordedCommand command in check)
        {
            plans.Add(await QueryPlan.ExplainAsync(connection, transaction, command));
        }

        string plan = string.Join(Environment.NewLine, plans);

        plan.Should()
            .Contain("idx_transactions_account_id")
            .And.Contain("idx_transactions_category_id")
            .And.Contain("idx_transfers_source_account_id")
            .And.Contain("idx_transfers_destination_account_id");
    }

    private static DateOnly Day(string day) =>
        DateOnly.ParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static Transaction Movement(Guid accountId, Guid categoryId, decimal amount, string day) =>
        MovementOn(accountId, categoryId, amount, Day(day));

    private static Transaction MovementOn(Guid accountId, Guid categoryId, decimal amount, DateOnly day) =>
        Transaction.Record(
            LedgerData.NewId(),
            accountId,
            categoryId,
            new Money(amount),
            day,
            note: null,
            DateTimeOffset.UtcNow);

    private static Transfer Move(Guid sourceAccountId, Guid destinationAccountId, decimal amount, string day) =>
        MoveOn(sourceAccountId, destinationAccountId, amount, Day(day));

    private static Transfer MoveOn(Guid sourceAccountId, Guid destinationAccountId, decimal amount, DateOnly day) =>
        Transfer.Record(
            LedgerData.NewId(),
            sourceAccountId,
            destinationAccountId,
            new Money(amount),
            day,
            DateTimeOffset.UtcNow);

    // A year of hand-typed movements on the owner's own accounts, so the run's created account
    // and category are a small share of both tables and the plan reflects a real choice.
    private async Task<IReadOnlyList<RecordedCommand>> RecordCheckAsync()
    {
        Account wallet = LedgerData.NewAccount("Cash wallet");
        Account jar = LedgerData.NewAccount("Savings jar");
        Account createdByRun = LedgerData.NewAccount("Imported card");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);
        Category createdCategory = LedgerData.NewCategory("Pharmacy", CategoryKind.Expense);
        DateOnly firstDay = Day("2026-01-01");

        Transaction[] yearOfSpending =
        [
            .. Enumerable.Range(0, 365).Select(offset =>
                MovementOn(wallet.Id, groceries.Id, 10m, firstDay.AddDays(offset))),
        ];
        Transfer[] yearOfSaving =
        [
            .. Enumerable.Range(0, 365).Select(offset =>
                MoveOn(wallet.Id, jar.Id, 5m, firstDay.AddDays(offset))),
        ];
        Transaction recordedByRun = Movement(createdByRun.Id, createdCategory.Id, 30m, "2026-03-05");
        Transfer movedByRun = Move(createdByRun.Id, jar.Id, 100m, "2026-03-06");

        await StoreAsync(
            [wallet, jar, createdByRun],
            [groceries, createdCategory],
            [.. yearOfSpending, recordedByRun],
            [.. yearOfSaving, movedByRun]);
        ImportRun run = await StoreRunAsync([createdByRun], [createdCategory], [recordedByRun], [movedByRun]);

        await using (var connection = await _database.OpenConnectionAsync())
        await using (var analyze = new NpgsqlCommand("ANALYZE", connection))
        {
            await analyze.ExecuteNonQueryAsync();
        }

        var commands = new RecordingCommandInterceptor();

        await using (var context = _database.CreateDbContext(commands))
        {
            await new EfImportQueries(context).GetCreationsNamedOutsideRunAsync(run.Id, CancellationToken.None);
        }

        return commands.Recorded;
    }

    private async Task<ImportRunCreationsInUse> CheckAsync(Guid importRunId)
    {
        await using var context = _database.CreateDbContext();

        return await new EfImportQueries(context).GetCreationsNamedOutsideRunAsync(importRunId, CancellationToken.None);
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

    private async Task<ImportRun> StoreRunAsync(
        IReadOnlyList<Account> createdAccounts,
        IReadOnlyList<Category> createdCategories,
        IReadOnlyList<Transaction> recordedTransactions,
        IReadOnlyList<Transfer> recordedTransfers)
    {
        ImportRun run = ImportRun.Record(
            LedgerData.NewId(),
            "history-export.csv",
            ImportRunKind.HistoryMigration,
            accountId: null,
            HandedOverAt);

        foreach (Account account in createdAccounts)
        {
            run.LinkCreatedAccount(LedgerData.NewId(), account);
        }

        foreach (Category category in createdCategories)
        {
            run.LinkCreatedCategory(LedgerData.NewId(), category);
        }

        foreach (Transaction transaction in recordedTransactions)
        {
            run.LinkRecordedTransaction(LedgerData.NewId(), transaction);
        }

        foreach (Transfer transfer in recordedTransfers)
        {
            run.LinkRecordedTransfer(LedgerData.NewId(), transfer);
        }

        await using var context = _database.CreateDbContext();

        new EfImportRunRepository(context).Add(run);

        await new UnitOfWork(context).SaveChangesAsync(CancellationToken.None);

        return run;
    }
}
