using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Infrastructure.Persistence;
using FinNavis.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinNavis.Infrastructure.IntegrationTests.Repositories;

[Collection(DatabaseCollection.Name)]
public sealed class EfImportRunRepositoryTests : IAsyncLifetime
{
    private const int RunTable = 1;

    private const int ChildTablesOfARun = 5;

    private static readonly DateTimeOffset HandedOverAt = new(2026, 10, 31, 9, 0, 0, TimeSpan.Zero);

    private readonly PostgresFixture _database;

    public EfImportRunRepositoryTests(PostgresFixture database) => _database = database;

    public Task InitializeAsync() => _database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Add_RunWithEveryKindOfChild_ReadsTheRunAndEveryChildRowBack()
    {
        StagedLoad load = NewStatementLoad(childrenPerCollection: 2);

        await StoreAsync(load);

        await using var read = _database.CreateDbContext();
        ImportRun? stored = await new EfImportRunRepository(read)
            .GetByIdAsync(load.Run.Id, CancellationToken.None);

        stored.Should().NotBeNull();
        stored!.FileName.Should().Be(load.Run.FileName);
        stored.Kind.Should().Be(ImportRunKind.Statement);
        stored.Status.Should().Be(ImportRunStatus.Completed);
        stored.AccountId.Should().Be(load.StatementAccount.Id);
        stored.RecordedRowCount.Should().Be(4);
        stored.AlreadyPresentRowCount.Should().Be(1);
        stored.CreatedAt.Should().Be(HandedOverAt);

        stored.TransactionLinks.Should().BeEquivalentTo(load.Run.TransactionLinks);
        stored.TransferLinks.Should().BeEquivalentTo(load.Run.TransferLinks);
        stored.CreatedAccountLinks.Should().BeEquivalentTo(load.Run.CreatedAccountLinks);
        stored.CreatedCategoryLinks.Should().BeEquivalentTo(load.Run.CreatedCategoryLinks);
        stored.SetAsideRows.Should().BeEquivalentTo(load.Run.SetAsideRows);
    }

    [Fact]
    public async Task GetByIdAsync_RunWithSeveralRowsPerChild_LoadsEveryChildWithAtMostOneCommandPerTable()
    {
        const int childrenPerCollection = 3;
        StagedLoad load = NewStatementLoad(childrenPerCollection);

        await StoreAsync(load);

        var commands = new RecordingCommandInterceptor();
        ImportRun? stored;

        // The context is gone before the collections are read, so nothing can be fetched later:
        // every child has to arrive inside the one call.
        await using (var read = _database.CreateDbContext(commands))
        {
            stored = await new EfImportRunRepository(read)
                .GetByIdAsync(load.Run.Id, CancellationToken.None);
        }

        commands.Recorded.Should().HaveCountLessThanOrEqualTo(RunTable + ChildTablesOfARun);

        stored.Should().NotBeNull();
        stored!.TransactionLinks.Should().HaveCount(childrenPerCollection);
        stored.TransferLinks.Should().HaveCount(childrenPerCollection);
        stored.CreatedAccountLinks.Should().HaveCount(childrenPerCollection);
        stored.CreatedCategoryLinks.Should().HaveCount(childrenPerCollection);
        stored.SetAsideRows.Should().HaveCount(childrenPerCollection);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        await using var read = _database.CreateDbContext();

        ImportRun? stored = await new EfImportRunRepository(read)
            .GetByIdAsync(LedgerData.NewId(), CancellationToken.None);

        stored.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_RunMarkedUndoneAndCommitted_ReadsTheUndoneStatusBack()
    {
        StagedLoad load = NewStatementLoad(childrenPerCollection: 1);

        await StoreAsync(load);

        await using (var write = _database.CreateDbContext())
        {
            ImportRun loaded = (await new EfImportRunRepository(write)
                .GetByIdAsync(load.Run.Id, CancellationToken.None))!;

            loaded.MarkUndone();
            await new UnitOfWork(write).SaveChangesAsync(CancellationToken.None);
        }

        await using var read = _database.CreateDbContext();
        ImportRun stored = (await new EfImportRunRepository(read)
            .GetByIdAsync(load.Run.Id, CancellationToken.None))!;

        stored.Status.Should().Be(ImportRunStatus.Undone);
    }

    // A failed load must leave nothing an owner could mistake for a half-done import. The
    // refusal is placed on a set-aside row on purpose: PostgreSQL can insert that row only after
    // the run row, so the run row is already written when the commit fails and has to be taken
    // back with everything else.
    [Fact]
    public async Task Add_WhenTheCommitIsRefused_LeavesNoRunLinkOrSetAsideRowBehind()
    {
        StagedLoad earlier = NewStatementLoad(childrenPerCollection: 1);

        await StoreAsync(earlier);

        Transaction movement = LedgerData.NewTransaction(
            earlier.StatementAccount.Id,
            earlier.CreatedCategories.Single().Id,
            42m);
        ImportRun refused = ImportRun.Record(
            LedgerData.NewId(),
            "statement-november.csv",
            ImportRunKind.Statement,
            earlier.StatementAccount.Id,
            HandedOverAt);
        refused.LinkRecordedTransaction(LedgerData.NewId(), movement);
        refused.SetAside(LedgerData.NewId(), 3, SetAsideReason.NoRuleCovers, "Synthetic shop", "2026-11-01;Synthetic shop;-3.00");
        refused.SetAside(earlier.Run.SetAsideRows.Single().Id, 4, SetAsideReason.ForeignCurrency, null, "2026-11-02;Synthetic cafe;-4.00 EUR");

        await using (var write = _database.CreateDbContext())
        {
            new TransactionRepository(write).Add(movement);
            new EfImportRunRepository(write).Add(refused);

            Func<Task> commit = () => new UnitOfWork(write).SaveChangesAsync(CancellationToken.None);

            await commit.Should().ThrowAsync<DbUpdateException>();
        }

        (await ImportTables.CountAsync(_database, "import_runs", "id", refused.Id)).Should().Be(0);
        (await ImportTables.CountAsync(_database, "import_transaction_links", "import_run_id", refused.Id)).Should().Be(0);
        (await ImportTables.CountAsync(_database, "import_set_aside_rows", "import_run_id", refused.Id)).Should().Be(0);
        (await ImportTables.CountAsync(_database, "transactions", "id", movement.Id)).Should().Be(0);
    }

    private static StagedLoad NewStatementLoad(int childrenPerCollection)
    {
        Account statementAccount = LedgerData.NewAccount("Main card");
        ImportRun run = ImportRun.Record(
            LedgerData.NewId(),
            "statement-october.csv",
            ImportRunKind.Statement,
            statementAccount.Id,
            HandedOverAt);

        List<Account> createdAccounts = [];
        List<Category> createdCategories = [];
        List<Transaction> transactions = [];
        List<Transfer> transfers = [];

        for (int index = 1; index <= childrenPerCollection; index++)
        {
            Account createdAccount = LedgerData.NewAccount($"Savings {index}");
            Category createdCategory = LedgerData.NewCategory($"Imported {index}", CategoryKind.Expense);
            Transaction transaction = LedgerData.NewTransaction(
                statementAccount.Id, createdCategory.Id, 10m + index, $"Synthetic shop {index}");
            Transfer transfer = LedgerData.NewTransfer(statementAccount.Id, createdAccount.Id, 100m + index);

            run.LinkCreatedAccount(LedgerData.NewId(), createdAccount);
            run.LinkCreatedCategory(LedgerData.NewId(), createdCategory);
            run.LinkRecordedTransaction(LedgerData.NewId(), transaction);
            run.LinkRecordedTransfer(LedgerData.NewId(), transfer);
            run.SetAside(
                LedgerData.NewId(),
                index + 1,
                index % 2 == 0 ? SetAsideReason.ForeignCurrency : SetAsideReason.NoRuleCovers,
                index % 2 == 0 ? null : $"Uncovered category {index}",
                $"2026-10-{index:00};Synthetic cafe {index};-{index}.50");

            createdAccounts.Add(createdAccount);
            createdCategories.Add(createdCategory);
            transactions.Add(transaction);
            transfers.Add(transfer);
        }

        run.CountAlreadyPresentRow();

        return new StagedLoad(statementAccount, createdAccounts, createdCategories, transactions, transfers, run);
    }

    // Everything one load did goes into one unit, the way the import use cases commit it.
    private async Task StoreAsync(StagedLoad load)
    {
        await using var context = _database.CreateDbContext();

        var accounts = new AccountRepository(context);
        var categories = new CategoryRepository(context);
        var transactionRepository = new TransactionRepository(context);
        var transferRepository = new TransferRepository(context);

        accounts.Add(load.StatementAccount);
        load.CreatedAccounts.ForEach(accounts.Add);
        load.CreatedCategories.ForEach(categories.Add);
        load.Transactions.ForEach(transactionRepository.Add);
        load.Transfers.ForEach(transferRepository.Add);
        new EfImportRunRepository(context).Add(load.Run);

        await new UnitOfWork(context).SaveChangesAsync(CancellationToken.None);
    }

    private sealed record StagedLoad(
        Account StatementAccount,
        List<Account> CreatedAccounts,
        List<Category> CreatedCategories,
        List<Transaction> Transactions,
        List<Transfer> Transfers,
        ImportRun Run);
}
