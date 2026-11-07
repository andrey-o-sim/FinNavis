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
public sealed class CandidateSnapshotTests : IAsyncLifetime
{
    private const string StatementLoadIndex = "idx_transactions_occurred_on_account_id";

    private static readonly DateRange March = DateRange.Create(Day("2026-03-01"), Day("2026-03-31"));

    private static readonly DateTimeOffset HandedOverAt = new(2026, 4, 2, 9, 0, 0, TimeSpan.Zero);

    private readonly PostgresFixture _database;

    public CandidateSnapshotTests(PostgresFixture database) => _database = database;

    public Task InitializeAsync() => _database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // Loading the same file twice and typing a purchase by hand before its statement arrives are
    // both everyday cases. A snapshot that only held one of the two origins would let the other
    // one be recorded twice.
    [Fact]
    public async Task GetCandidateSnapshotAsync_MovementsAnEarlierRunRecordedAndMovementsTypedByHand_ReturnsBoth()
    {
        Account card = LedgerData.NewAccount("Debit card");
        Account jar = LedgerData.NewAccount("Savings jar");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);

        Transaction typedByHand = Movement(card.Id, groceries.Id, 30m, "2026-03-05");
        Transfer movedByHand = Move(card.Id, jar.Id, 100m, "2026-03-06");
        Transaction recordedByRun = Movement(card.Id, groceries.Id, 45m, "2026-03-07");
        Transfer movedByRun = Move(jar.Id, card.Id, 20m, "2026-03-08");

        await StoreAsync([card, jar], [groceries], [typedByHand, recordedByRun], [movedByHand, movedByRun]);
        await StoreRunAsync(card.Id, [recordedByRun], [movedByRun]);
        await SetExternalRefAsync(recordedByRun.Id, "bank-ref-0001");

        CandidateSnapshot snapshot = await TakeSnapshotAsync([card.Id], March);

        snapshot.Transactions.Select(row => row.Id).Should().BeEquivalentTo([typedByHand.Id, recordedByRun.Id]);
        snapshot.Transfers.Select(row => row.Id).Should().BeEquivalentTo([movedByHand.Id, movedByRun.Id]);
    }

    [Fact]
    public async Task GetCandidateSnapshotAsync_TransactionInRange_ReadsEveryFieldTheMatcherNeeds()
    {
        Account card = LedgerData.NewAccount("Debit card");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);
        Transaction purchase = Transaction.Record(
            LedgerData.NewId(),
            card.Id,
            groceries.Id,
            new Money(12.3456m),
            Day("2026-03-05"),
            "Corner shop",
            DateTimeOffset.UtcNow);

        await StoreAsync([card], [groceries], [purchase], []);
        await SetExternalRefAsync(purchase.Id, "bank-ref-0002");

        CandidateSnapshot snapshot = await TakeSnapshotAsync([card.Id], March);

        snapshot.Transactions.Should().ContainSingle().Which.Should().Be(new CandidateTransactionRow(
            purchase.Id,
            Day("2026-03-05"),
            new Money(12.3456m),
            card.Id,
            groceries.Id,
            CategoryKind.Expense,
            "Corner shop",
            "bank-ref-0002"));
    }

    [Fact]
    public async Task GetCandidateSnapshotAsync_TransferInRange_ReadsEveryFieldTheMatcherNeeds()
    {
        Account card = LedgerData.NewAccount("Debit card");
        Account jar = LedgerData.NewAccount("Savings jar");
        Transfer saving = Move(card.Id, jar.Id, 250.5m, "2026-03-10");

        await StoreAsync([card, jar], [], [], [saving]);

        CandidateSnapshot snapshot = await TakeSnapshotAsync([card.Id], March);

        snapshot.Transfers.Should().ContainSingle().Which.Should().Be(new CandidateTransferRow(
            saving.Id,
            Day("2026-03-10"),
            new Money(250.5m),
            card.Id,
            jar.Id));
    }

    // One statement over both tables is what keeps the snapshot a single picture of the ledger.
    // The command is caught by an EF Core interceptor, so the snapshot has to go through the
    // context's command pipeline for this to see it.
    [Fact]
    public async Task GetCandidateSnapshotAsync_StatementLoad_IssuesExactlyOneReadCommand()
    {
        Account card = LedgerData.NewAccount("Debit card");
        Account jar = LedgerData.NewAccount("Savings jar");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);

        await StoreAsync(
            [card, jar],
            [groceries],
            [Movement(card.Id, groceries.Id, 30m, "2026-03-05")],
            [Move(card.Id, jar.Id, 100m, "2026-03-06")]);

        var commands = new RecordingCommandInterceptor();

        await using (var context = _database.CreateDbContext(commands))
        {
            await new EfImportQueries(context).GetCandidateSnapshotAsync([card.Id], March, CancellationToken.None);
        }

        RecordedCommand snapshotCommand = commands.Recorded.Should().ContainSingle().Subject;
        snapshotCommand.Text.Should().Contain("transactions").And.Contain("transfers");
        snapshotCommand.Text.Should().NotContainAny("INSERT", "UPDATE", "DELETE");
    }

    // Two identical spends in one file are both real. The run writes the first one after the
    // snapshot was taken, and the second one must not find it there.
    [Fact]
    public async Task GetCandidateSnapshotAsync_MovementRecordedAfterTheSnapshotWasTaken_IsNotInTheSnapshot()
    {
        Account card = LedgerData.NewAccount("Debit card");
        Account jar = LedgerData.NewAccount("Savings jar");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);
        Transaction before = Movement(card.Id, groceries.Id, 30m, "2026-03-05");

        await StoreAsync([card, jar], [groceries], [before], []);

        CandidateSnapshot snapshot = await TakeSnapshotAsync([card.Id], March);

        await StoreAsync(
            [],
            [],
            [Movement(card.Id, groceries.Id, 30m, "2026-03-05")],
            [Move(card.Id, jar.Id, 100m, "2026-03-06")]);

        snapshot.Transactions.Select(row => row.Id).Should().Equal(before.Id);
        snapshot.Transfers.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCandidateSnapshotAsync_MovementsOutsideTheRange_LeavesThemOut()
    {
        Account card = LedgerData.NewAccount("Debit card");
        Account jar = LedgerData.NewAccount("Savings jar");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);

        Transaction dayBefore = Movement(card.Id, groceries.Id, 10m, "2026-02-28");
        Transaction firstDay = Movement(card.Id, groceries.Id, 20m, "2026-03-01");
        Transaction lastDay = Movement(card.Id, groceries.Id, 40m, "2026-03-31");
        Transaction dayAfter = Movement(card.Id, groceries.Id, 80m, "2026-04-01");
        Transfer transferBefore = Move(card.Id, jar.Id, 5m, "2026-02-28");
        Transfer transferFirstDay = Move(card.Id, jar.Id, 6m, "2026-03-01");
        Transfer transferLastDay = Move(jar.Id, card.Id, 7m, "2026-03-31");
        Transfer transferAfter = Move(jar.Id, card.Id, 8m, "2026-04-01");

        await StoreAsync(
            [card, jar],
            [groceries],
            [dayBefore, firstDay, lastDay, dayAfter],
            [transferBefore, transferFirstDay, transferLastDay, transferAfter]);

        CandidateSnapshot snapshot = await TakeSnapshotAsync([card.Id], March);

        // Both ends of the range count as inside.
        snapshot.Transactions.Select(row => row.Id).Should().BeEquivalentTo([firstDay.Id, lastDay.Id]);
        snapshot.Transfers.Select(row => row.Id).Should().BeEquivalentTo([transferFirstDay.Id, transferLastDay.Id]);
    }

    // A statement row for the card can be either side of a transfer, so a transfer naming the
    // card as source or as destination is a candidate, and one between two other accounts is not.
    [Fact]
    public async Task GetCandidateSnapshotAsync_MovementsOnAnotherAccount_LeavesThemOut()
    {
        Account card = LedgerData.NewAccount("Debit card");
        Account wallet = LedgerData.NewAccount("Cash wallet");
        Account jar = LedgerData.NewAccount("Savings jar");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);

        Transaction onTheCard = Movement(card.Id, groceries.Id, 30m, "2026-03-05");
        Transaction inTheWallet = Movement(wallet.Id, groceries.Id, 30m, "2026-03-05");
        Transfer outOfTheCard = Move(card.Id, jar.Id, 100m, "2026-03-06");
        Transfer intoTheCard = Move(wallet.Id, card.Id, 50m, "2026-03-07");
        Transfer betweenOthers = Move(wallet.Id, jar.Id, 70m, "2026-03-08");

        await StoreAsync(
            [card, wallet, jar],
            [groceries],
            [onTheCard, inTheWallet],
            [outOfTheCard, intoTheCard, betweenOthers]);

        CandidateSnapshot snapshot = await TakeSnapshotAsync([card.Id], March);

        snapshot.Transactions.Select(row => row.Id).Should().Equal(onTheCard.Id);
        snapshot.Transfers.Select(row => row.Id).Should().BeEquivalentTo([outOfTheCard.Id, intoTheCard.Id]);
    }

    [Fact]
    public async Task GetCandidateSnapshotAsync_HistoryMigrationOverSeveralAccounts_ReturnsMovementsOfEveryNamedAccount()
    {
        Account card = LedgerData.NewAccount("Debit card");
        Account wallet = LedgerData.NewAccount("Cash wallet");
        Account jar = LedgerData.NewAccount("Savings jar");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);
        Category salary = LedgerData.NewCategory("Salary", CategoryKind.Income);
        DateRange threeYears = DateRange.Create(Day("2024-01-01"), Day("2026-12-31"));

        Transaction cardSpend = Movement(card.Id, groceries.Id, 30m, "2024-02-10");
        Transaction walletEarning = Movement(wallet.Id, salary.Id, 900m, "2025-06-01");
        Transaction jarSpend = Movement(jar.Id, groceries.Id, 15m, "2026-11-20");
        Transfer cardToWallet = Move(card.Id, wallet.Id, 40m, "2025-01-15");
        Transfer walletToJar = Move(wallet.Id, jar.Id, 60m, "2026-03-03");

        await StoreAsync(
            [card, wallet, jar],
            [groceries, salary],
            [cardSpend, walletEarning, jarSpend],
            [cardToWallet, walletToJar]);

        CandidateSnapshot snapshot = await TakeSnapshotAsync([card.Id, wallet.Id], threeYears);

        snapshot.Transactions.Select(row => row.Id).Should().BeEquivalentTo([cardSpend.Id, walletEarning.Id]);
        snapshot.Transfers.Select(row => row.Id).Should().BeEquivalentTo([cardToWallet.Id, walletToJar.Id]);
    }

    // Only the monthly shape is planned: a history migration reads close to the whole table, and
    // PostgreSQL rightly reads that sequentially whatever index exists.
    [Fact]
    public async Task GetCandidateSnapshotAsync_StatementLoad_IsPlannedOnTheOccurredOnAccountIndex()
    {
        RecordedCommand snapshot = await RecordStatementLoadSnapshotAsync();

        await using var connection = await _database.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        string plan = await QueryPlan.ExplainAsync(connection, transaction, snapshot);

        plan.Should().Contain(StatementLoadIndex);
    }

    // Proves the plan check above can fail. The drop is rolled back, so the schema is unchanged
    // for every other test.
    [Fact]
    public async Task GetCandidateSnapshotAsync_WithTheOccurredOnAccountIndexDropped_IsPlannedWithoutIt()
    {
        RecordedCommand snapshot = await RecordStatementLoadSnapshotAsync();

        await using var connection = await _database.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await using (var drop = new NpgsqlCommand($"DROP INDEX {StatementLoadIndex}", connection, transaction))
        {
            await drop.ExecuteNonQueryAsync();
        }

        string plan = await QueryPlan.ExplainAsync(connection, transaction, snapshot);

        await transaction.RollbackAsync();

        plan.Should().NotContain(StatementLoadIndex);
    }

    // The ledger tables keep exactly the indexes the ledger itself declared. The snapshot is
    // served by those, so a new index here is a schema change nobody decided on.
    [Fact]
    public async Task LedgerTables_AfterEveryMigration_CarryOnlyTheLedgerOwnIndexes()
    {
        await using var connection = await _database.OpenConnectionAsync();
        await using var read = new NpgsqlCommand(
            "SELECT indexname FROM pg_indexes WHERE schemaname = 'public' AND tablename IN ('transactions', 'transfers')",
            connection);

        List<string> indexes = [];

        await using (var reader = await read.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                indexes.Add(reader.GetString(0));
            }
        }

        indexes.Should().BeEquivalentTo(
        [
            "pk_transactions",
            "idx_transactions_occurred_on_account_id",
            "idx_transactions_account_id",
            "idx_transactions_category_id",
            "ux_transactions_external_ref",
            "pk_transfers",
            "idx_transfers_source_account_id",
            "idx_transfers_destination_account_id",
            "idx_transfers_occurred_on",
        ]);
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
        Transfer.Record(
            LedgerData.NewId(),
            sourceAccountId,
            destinationAccountId,
            new Money(amount),
            Day(day),
            DateTimeOffset.UtcNow);

    // A year of daily spending on two accounts, so a month on one account is a small share of the
    // table and the plan reflects a real choice between the indexes.
    private async Task<RecordedCommand> RecordStatementLoadSnapshotAsync()
    {
        Account card = LedgerData.NewAccount("Debit card");
        Account wallet = LedgerData.NewAccount("Cash wallet");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);
        DateOnly firstDay = Day("2026-01-01");

        Transaction[] yearOfSpending =
        [
            .. Enumerable.Range(0, 365).SelectMany(offset => new[]
            {
                MovementOn(card.Id, groceries.Id, 10m, firstDay.AddDays(offset)),
                MovementOn(wallet.Id, groceries.Id, 10m, firstDay.AddDays(offset)),
            }),
        ];

        await StoreAsync([card, wallet], [groceries], yearOfSpending, []);

        await using (var connection = await _database.OpenConnectionAsync())
        await using (var analyze = new NpgsqlCommand("ANALYZE transactions, transfers", connection))
        {
            await analyze.ExecuteNonQueryAsync();
        }

        var commands = new RecordingCommandInterceptor();

        await using (var context = _database.CreateDbContext(commands))
        {
            await new EfImportQueries(context).GetCandidateSnapshotAsync([card.Id], March, CancellationToken.None);
        }

        return commands.Recorded.Single();
    }

    private async Task<CandidateSnapshot> TakeSnapshotAsync(IReadOnlyCollection<Guid> accountIds, DateRange period)
    {
        await using var context = _database.CreateDbContext();

        return await new EfImportQueries(context).GetCandidateSnapshotAsync(accountIds, period, CancellationToken.None);
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

    private async Task StoreRunAsync(
        Guid statementAccountId,
        IReadOnlyList<Transaction> recordedTransactions,
        IReadOnlyList<Transfer> recordedTransfers)
    {
        ImportRun run = ImportRun.Record(
            LedgerData.NewId(),
            "statement-2026-03.csv",
            ImportRunKind.Statement,
            statementAccountId,
            HandedOverAt);

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
    }

    // The domain gives a transaction no way to carry a bank reference yet, so it is written
    // around EF Core, the way an earlier import would have left it.
    private async Task SetExternalRefAsync(Guid transactionId, string externalRef)
    {
        await using var connection = await _database.OpenConnectionAsync();
        await using var update = new NpgsqlCommand(
            "UPDATE transactions SET external_ref = @external_ref WHERE id = @id",
            connection);

        update.Parameters.AddWithValue("external_ref", externalRef);
        update.Parameters.AddWithValue("id", transactionId);

        await update.ExecuteNonQueryAsync();
    }
}
