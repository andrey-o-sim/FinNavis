using FinNavis.Application.Abstractions.Reads;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;
using UndoImportRunUseCase = FinNavis.Application.Import.UndoImportRun.UndoImportRun;

namespace FinNavis.Application.UnitTests.Import.UndoImportRun;

public sealed class UndoImportRunTests
{
    private const string ChangedByHandMessage =
        "Some rows from this load were changed by hand afterwards, so the load cannot be undone as a whole.";

    private static readonly DateTimeOffset LoadedAt = new(2026, 11, 7, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly StatementDay = new(2026, 10, 15);

    private readonly FakeImportRunRepository _runs = new();
    private readonly FakeTransactionRepository _transactions = new();
    private readonly FakeTransferRepository _transfers = new();
    private readonly FakeAccountRepository _accounts = new();
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeImportQueries _importQueries;
    private readonly FakeLedgerQueries _ledger;
    private readonly FakeUnitOfWork _unitOfWork;

    public UndoImportRunTests()
    {
        _importQueries = new FakeImportQueries(_runs, _transactions, _transfers, _categories);
        _ledger = new FakeLedgerQueries(_accounts, _transactions, _categories, _transfers);
        _unitOfWork = new FakeUnitOfWork(() =>
        {
            _transactions.Commit();
            _transfers.Commit();
            _accounts.Commit();
            _categories.Commit();
        });
    }

    [Fact]
    public async Task ExecuteAsync_RunAlreadyUndone_IsRefusedAsAlreadyUndone()
    {
        Account wallet = SeedAccount("Test wallet");
        Category groceries = SeedCategory("Test groceries", CategoryKind.Expense);
        ImportRun run = StatementRun(wallet);
        RecordTransaction(run, wallet, groceries, 12.5000m);
        run.MarkUndone();
        _runs.Seed(run);

        Func<Task> undo = () => UndoAsync(run.Id);

        DomainRuleViolationException refusal =
            (await undo.Should().ThrowAsync<DomainRuleViolationException>()).Which;
        refusal.Code.Should().Be("Import.AlreadyUndone");
        refusal.Message.Should().Be("This load was already undone, so there is nothing left to remove.");
    }

    // A second undo must not touch anything, even a movement that somehow still points back at the
    // run: the first undo already said what the load did, and the run stays as it was left.
    [Fact]
    public async Task ExecuteAsync_RunAlreadyUndone_ChangesNothing()
    {
        Account wallet = SeedAccount("Test wallet");
        Category groceries = SeedCategory("Test groceries", CategoryKind.Expense);
        ImportRun run = StatementRun(wallet);
        Transaction lingering = RecordTransaction(run, wallet, groceries, 12.5000m);
        run.MarkUndone();
        _runs.Seed(run);

        Func<Task> undo = () => UndoAsync(run.Id);

        await undo.Should().ThrowAsync<DomainRuleViolationException>();
        run.Status.Should().Be(ImportRunStatus.Undone);
        _transactions.Stored.Should().ContainSingle().Which.Id.Should().Be(lingering.Id);
        _transactions.StagedForRemoval.Should().BeEmpty();
        _accounts.StagedForRemoval.Should().BeEmpty();
        _categories.StagedForRemoval.Should().BeEmpty();
        _unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownRunId_IsRefusedAsImportNotFound()
    {
        Guid unknownId = Guid.CreateVersion7();

        Func<Task> undo = () => UndoAsync(unknownId);

        DomainRuleViolationException refusal =
            (await undo.Should().ThrowAsync<DomainRuleViolationException>()).Which;
        refusal.Code.Should().Be("Import.NotFound");
        refusal.Message.Should().Be($"No load in this installation has id {unknownId}.");
        _unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_TransactionChangedByHand_IsRefusedAndNamesTheChangedRows()
    {
        Account wallet = SeedAccount("Test wallet");
        Account savings = SeedAccount("Test savings");
        Category groceries = SeedCategory("Test groceries", CategoryKind.Expense);
        ImportRun run = StatementRun(wallet);
        Transaction untouched = RecordTransaction(run, wallet, groceries, 12.5000m);
        Transaction editedTransaction = RecordTransaction(run, wallet, groceries, 30.0000m);
        Transfer editedTransfer = RecordTransfer(run, wallet, savings, 100.0000m);
        _runs.Seed(run);
        editedTransaction.Edit(wallet.Id, null, groceries.Id, new Money(31.0000m), StatementDay, note: null);
        editedTransfer.Edit(wallet.Id, savings.Id, new Money(100.0000m), StatementDay.AddDays(1));

        Func<Task> undo = () => UndoAsync(run.Id);

        DomainRuleViolationException refusal =
            (await undo.Should().ThrowAsync<DomainRuleViolationException>()).Which;
        refusal.Code.Should().Be("Import.MovementsChangedByHand");
        refusal.Message.Should().Be(ChangedByHandMessage);
        refusal.Details.Should().NotBeNull();
        refusal.Details!["transaction_ids"].Should().BeAssignableTo<IEnumerable<Guid>>()
            .Which.Should().BeEquivalentTo([editedTransaction.Id]);
        refusal.Details["transfer_ids"].Should().BeAssignableTo<IEnumerable<Guid>>()
            .Which.Should().BeEquivalentTo([editedTransfer.Id]);
        refusal.Details["transaction_ids"].As<IEnumerable<Guid>>().Should().NotContain(untouched.Id);
    }

    // The refusal is all-or-nothing: if even one row stops the undo, the rows that were not edited
    // must survive too, or the owner is left with half a load and no way to finish undoing it.
    [Fact]
    public async Task ExecuteAsync_OneMovementChangedByHand_DeletesNothingAtAllAndKeepsTheRunCompleted()
    {
        Account wallet = SeedAccount("Test wallet");
        Category groceries = SeedCategory("Test groceries", CategoryKind.Expense);
        ImportRun run = HistoryRun();
        Account createdAccount = CreateByRun(run, "Test created account");
        Category createdCategory = CreateByRun(run, "Test created category", CategoryKind.Expense);
        Transaction untouched = RecordTransaction(run, createdAccount, createdCategory, 12.5000m);
        Transaction edited = RecordTransaction(run, wallet, groceries, 30.0000m);
        _runs.Seed(run);
        edited.Edit(wallet.Id, null, groceries.Id, new Money(30.0000m), StatementDay, note: "edited by hand");

        Func<Task> undo = () => UndoAsync(run.Id);

        await undo.Should().ThrowAsync<DomainRuleViolationException>();
        run.Status.Should().Be(ImportRunStatus.Completed);
        _transactions.StagedForRemoval.Should().BeEmpty();
        _transfers.StagedForRemoval.Should().BeEmpty();
        _accounts.StagedForRemoval.Should().BeEmpty();
        _categories.StagedForRemoval.Should().BeEmpty();
        _transactions.Stored.Select(transaction => transaction.Id)
            .Should().BeEquivalentTo([untouched.Id, edited.Id]);
        _accounts.Stored.Should().Contain(createdAccount);
        _categories.Stored.Should().Contain(createdCategory);
        _unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_TransferChangedByHand_IsRefusedAndDeletesNothing()
    {
        Account wallet = SeedAccount("Test wallet");
        Account savings = SeedAccount("Test savings");
        Category groceries = SeedCategory("Test groceries", CategoryKind.Expense);
        ImportRun run = StatementRun(wallet);
        RecordTransaction(run, wallet, groceries, 12.5000m);
        Transfer edited = RecordTransfer(run, wallet, savings, 100.0000m);
        _runs.Seed(run);
        edited.Edit(wallet.Id, savings.Id, new Money(150.0000m), StatementDay);

        Func<Task> undo = () => UndoAsync(run.Id);

        DomainRuleViolationException refusal =
            (await undo.Should().ThrowAsync<DomainRuleViolationException>()).Which;
        refusal.Code.Should().Be("Import.MovementsChangedByHand");
        refusal.Details!["transfer_ids"].Should().BeAssignableTo<IEnumerable<Guid>>()
            .Which.Should().BeEquivalentTo([edited.Id]);
        refusal.Details["transaction_ids"].Should().BeAssignableTo<IEnumerable<Guid>>()
            .Which.Should().BeEmpty();
        run.Status.Should().Be(ImportRunStatus.Completed);
        _transactions.StagedForRemoval.Should().BeEmpty();
        _transfers.StagedForRemoval.Should().BeEmpty();
        _unitOfWork.Commits.Should().Be(0);
    }

    // A movement the owner deleted by hand is simply absent. Treating the absence as a change would
    // refuse an undo that has every right to finish.
    [Fact]
    public async Task ExecuteAsync_MovementDeletedByHand_BlocksNothingAndTheRestIsRemoved()
    {
        Account wallet = SeedAccount("Test wallet");
        Account savings = SeedAccount("Test savings");
        Category groceries = SeedCategory("Test groceries", CategoryKind.Expense);
        ImportRun run = StatementRun(wallet);
        RecordTransaction(run, wallet, groceries, 12.5000m);
        Transaction deletedByHand = RecordTransaction(run, wallet, groceries, 30.0000m);
        Transfer deletedTransferByHand = RecordTransfer(run, wallet, savings, 100.0000m);
        _runs.Seed(run);
        _transactions.Remove(deletedByHand);
        _transfers.Remove(deletedTransferByHand);
        _transactions.Commit();
        _transfers.Commit();

        await UndoAsync(run.Id);

        run.Status.Should().Be(ImportRunStatus.Undone);
        _transactions.Stored.Should().BeEmpty();
        _transfers.Stored.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_CleanRun_RemovesEveryMovementTheRunRecordedAndMarksItUndone()
    {
        Account wallet = SeedAccount("Test wallet");
        Account savings = SeedAccount("Test savings");
        Category groceries = SeedCategory("Test groceries", CategoryKind.Expense);
        Category salary = SeedCategory("Test salary", CategoryKind.Income);
        Transaction before = SeedOutsideTransaction(wallet, groceries, 5.0000m);
        ImportRun run = StatementRun(wallet);
        RecordTransaction(run, wallet, groceries, 12.5000m);
        RecordTransaction(run, wallet, salary, 1000.0000m);
        RecordTransfer(run, wallet, savings, 100.0000m);
        _runs.Seed(run);

        await UndoAsync(run.Id);

        run.Status.Should().Be(ImportRunStatus.Undone);
        _transactions.Stored.Should().ContainSingle().Which.Id.Should().Be(before.Id);
        _transfers.Stored.Should().BeEmpty();
    }

    // No balance is stored, so the proof that every balance is back is the balance read itself:
    // after the undo it must equal what it was before the load wrote anything.
    [Fact]
    public async Task ExecuteAsync_CleanRun_EveryBalanceReturnsToWhatItWasBeforeTheLoad()
    {
        Account wallet = SeedAccount("Test wallet");
        Account savings = SeedAccount("Test savings");
        Category groceries = SeedCategory("Test groceries", CategoryKind.Expense);
        Category salary = SeedCategory("Test salary", CategoryKind.Income);
        SeedOutsideTransaction(wallet, salary, 500.0000m);
        SeedOutsideTransaction(wallet, groceries, 20.0000m);
        IReadOnlyList<AccountBalanceRow> beforeLoad = await ReadBalancesAsync();

        ImportRun run = StatementRun(wallet);
        RecordTransaction(run, wallet, groceries, 12.5000m);
        RecordTransaction(run, wallet, salary, 1000.0000m);
        RecordTransfer(run, wallet, savings, 100.0000m);
        _runs.Seed(run);
        IReadOnlyList<AccountBalanceRow> afterLoad = await ReadBalancesAsync();
        afterLoad.Should().NotBeEquivalentTo(beforeLoad);

        await UndoAsync(run.Id);

        IReadOnlyList<AccountBalanceRow> afterUndo = await ReadBalancesAsync();
        afterUndo.Should().BeEquivalentTo(beforeLoad);
    }

    // The set-aside rows were never in the ledger, so the undo has nothing to take back from them,
    // and the report keeps listing them.
    [Fact]
    public async Task ExecuteAsync_CleanRun_KeepsTheSetAsideRowsAndTheStoredCounts()
    {
        Account wallet = SeedAccount("Test wallet");
        Category groceries = SeedCategory("Test groceries", CategoryKind.Expense);
        ImportRun run = StatementRun(wallet);
        RecordTransaction(run, wallet, groceries, 12.5000m);
        run.SetAside(Guid.CreateVersion7(), 4, SetAsideReason.NoRuleCovers, "TEST HOBBIES", "line 4");
        _runs.Seed(run);

        await UndoAsync(run.Id);

        run.SetAsideRows.Should().ContainSingle().Which.RowNumber.Should().Be(4);
        run.RecordedRowCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_CreatedAccountAndCategoryNamedOnlyByTheRun_AreRemoved()
    {
        ImportRun run = HistoryRun();
        Account createdAccount = CreateByRun(run, "Test created account");
        Category createdCategory = CreateByRun(run, "Test created category", CategoryKind.Expense);
        RecordTransaction(run, createdAccount, createdCategory, 12.5000m);
        _runs.Seed(run);

        await UndoAsync(run.Id);

        _accounts.Stored.Should().NotContain(createdAccount);
        _categories.Stored.Should().NotContain(createdCategory);
        _importQueries.CreationsRequestedForRunIds.Should().Equal(run.Id);
    }

    // The owner may have started using something the load created — recording a spend by hand in a
    // created category, or moving money into a created account. Removing it would orphan that
    // movement, so it stays even though the load that created it is undone.
    [Fact]
    public async Task ExecuteAsync_CreatedAccountAndCategoryNamedFromOutsideTheRun_AreKept()
    {
        Account wallet = SeedAccount("Test wallet");
        ImportRun run = HistoryRun();
        Account usedAccount = CreateByRun(run, "Test used account");
        Account unusedAccount = CreateByRun(run, "Test unused account");
        Category usedCategory = CreateByRun(run, "Test used category", CategoryKind.Expense);
        Category unusedCategory = CreateByRun(run, "Test unused category", CategoryKind.Expense);
        RecordTransaction(run, usedAccount, usedCategory, 12.5000m);
        RecordTransaction(run, unusedAccount, unusedCategory, 7.0000m);
        _runs.Seed(run);
        Transaction byHand = SeedOutsideTransaction(wallet, usedCategory, 3.0000m);
        Transfer intoCreatedAccount = SeedOutsideTransfer(wallet, usedAccount, 50.0000m);

        await UndoAsync(run.Id);

        _accounts.Stored.Should().Contain(usedAccount);
        _categories.Stored.Should().Contain(usedCategory);
        _accounts.Stored.Should().NotContain(unusedAccount);
        _categories.Stored.Should().NotContain(unusedCategory);
        _transactions.Stored.Should().ContainSingle().Which.Id.Should().Be(byHand.Id);
        _transfers.Stored.Should().ContainSingle().Which.Id.Should().Be(intoCreatedAccount.Id);
    }

    // An account the owner already had, which the load only wrote onto, was never the load's to
    // remove.
    [Fact]
    public async Task ExecuteAsync_AccountTheRunDidNotCreate_IsKept()
    {
        Account wallet = SeedAccount("Test wallet");
        Category groceries = SeedCategory("Test groceries", CategoryKind.Expense);
        ImportRun run = StatementRun(wallet);
        RecordTransaction(run, wallet, groceries, 12.5000m);
        _runs.Seed(run);

        await UndoAsync(run.Id);

        _accounts.Stored.Should().Contain(wallet);
        _categories.Stored.Should().Contain(groceries);
    }

    // The duplicate check reads the candidate snapshot. With the run's movements gone it finds
    // nothing, so handing the same file over again records every row instead of calling them
    // already present.
    [Fact]
    public async Task ExecuteAsync_CleanRun_LeavesNothingForTheSameFileToMatchAgainst()
    {
        Account wallet = SeedAccount("Test wallet");
        Account savings = SeedAccount("Test savings");
        Category groceries = SeedCategory("Test groceries", CategoryKind.Expense);
        ImportRun run = StatementRun(wallet);
        RecordTransaction(run, wallet, groceries, 12.5000m);
        RecordTransaction(run, wallet, groceries, 30.0000m);
        RecordTransfer(run, wallet, savings, 100.0000m);
        _runs.Seed(run);
        DateRange statementPeriod = DateRange.Create(StatementDay.AddDays(-14), StatementDay.AddDays(14));

        await UndoAsync(run.Id);

        CandidateSnapshot snapshot = await _importQueries.GetCandidateSnapshotAsync(
            [wallet.Id], statementPeriod, CancellationToken.None);
        snapshot.Transactions.Should().BeEmpty();
        snapshot.Transfers.Should().BeEmpty();
        run.Status.Should().Be(ImportRunStatus.Undone);
    }

    [Fact]
    public async Task ExecuteAsync_CleanRun_CommitsEverythingInOneUnitOfWork()
    {
        Account wallet = SeedAccount("Test wallet");
        Account savings = SeedAccount("Test savings");
        Category groceries = SeedCategory("Test groceries", CategoryKind.Expense);
        ImportRun run = HistoryRun();
        Account createdAccount = CreateByRun(run, "Test created account");
        Category createdCategory = CreateByRun(run, "Test created category", CategoryKind.Expense);
        RecordTransaction(run, wallet, groceries, 12.5000m);
        RecordTransaction(run, createdAccount, createdCategory, 30.0000m);
        RecordTransfer(run, wallet, savings, 100.0000m);
        _runs.Seed(run);

        await UndoAsync(run.Id);

        _unitOfWork.Commits.Should().Be(1);
        _transactions.Stored.Should().BeEmpty();
        _transfers.Stored.Should().BeEmpty();
        _accounts.Stored.Should().NotContain(createdAccount);
        _categories.Stored.Should().NotContain(createdCategory);
    }

    [Fact]
    public async Task ExecuteAsync_CleanRun_PassesTheTokenThroughEveryCall()
    {
        Account wallet = SeedAccount("Test wallet");
        Category groceries = SeedCategory("Test groceries", CategoryKind.Expense);
        ImportRun run = StatementRun(wallet);
        RecordTransaction(run, wallet, groceries, 12.5000m);
        _runs.Seed(run);
        using var cancellation = new CancellationTokenSource();

        await NewUseCase().ExecuteAsync(run.Id, cancellation.Token);

        _runs.TokensSeen.Should().NotBeEmpty().And.OnlyContain(token => token == cancellation.Token);
        _transactions.TokensSeen.Should().NotBeEmpty().And.OnlyContain(token => token == cancellation.Token);
        _importQueries.TokensSeen.Should().NotBeEmpty().And.OnlyContain(token => token == cancellation.Token);
        _unitOfWork.TokensSeen.Should().Equal(cancellation.Token);
    }

    private static ImportRun StatementRun(Account account) =>
        ImportRun.Record(
            Guid.CreateVersion7(), "test-statement-2026-10.csv", ImportRunKind.Statement, account.Id, LoadedAt);

    private static ImportRun HistoryRun() =>
        ImportRun.Record(
            Guid.CreateVersion7(), "test-history.csv", ImportRunKind.HistoryMigration, accountId: null, LoadedAt);

    private Account SeedAccount(string name)
    {
        Account account = Account.Create(Guid.CreateVersion7(), name, LoadedAt.AddDays(-30));
        _accounts.Seed(account);

        return account;
    }

    private Category SeedCategory(string name, CategoryKind kind)
    {
        Category category = Category.Create(Guid.CreateVersion7(), name, kind, group: null, LoadedAt.AddDays(-30));
        _categories.Seed(category);

        return category;
    }

    private Account CreateByRun(ImportRun run, string name)
    {
        Account account = Account.Create(Guid.CreateVersion7(), name, LoadedAt);
        _accounts.Seed(account);
        run.LinkCreatedAccount(Guid.CreateVersion7(), account);

        return account;
    }

    private Category CreateByRun(ImportRun run, string name, CategoryKind kind)
    {
        Category category = Category.Create(Guid.CreateVersion7(), name, kind, group: null, LoadedAt);
        _categories.Seed(category);
        run.LinkCreatedCategory(Guid.CreateVersion7(), category);

        return category;
    }

    private Transaction RecordTransaction(ImportRun run, Account account, Category category, decimal amount)
    {
        Transaction transaction = Transaction.Record(
            Guid.CreateVersion7(), account.Id, category.Id, new Money(amount), StatementDay, note: null, LoadedAt);
        _transactions.Seed(transaction);
        run.LinkRecordedTransaction(Guid.CreateVersion7(), transaction);

        return transaction;
    }

    private Transfer RecordTransfer(ImportRun run, Account source, Account destination, decimal amount)
    {
        Transfer transfer = Transfer.Record(
            Guid.CreateVersion7(), source.Id, destination.Id, new Money(amount), StatementDay, LoadedAt);
        _transfers.Seed(transfer);
        run.LinkRecordedTransfer(Guid.CreateVersion7(), transfer);

        return transfer;
    }

    private Transaction SeedOutsideTransaction(Account account, Category category, decimal amount)
    {
        Transaction transaction = Transaction.Record(
            Guid.CreateVersion7(), account.Id, category.Id, new Money(amount), StatementDay, note: null, LoadedAt);
        _transactions.Seed(transaction);

        return transaction;
    }

    private Transfer SeedOutsideTransfer(Account source, Account destination, decimal amount)
    {
        Transfer transfer = Transfer.Record(
            Guid.CreateVersion7(), source.Id, destination.Id, new Money(amount), StatementDay, LoadedAt);
        _transfers.Seed(transfer);

        return transfer;
    }

    private Task<IReadOnlyList<AccountBalanceRow>> ReadBalancesAsync() =>
        _ledger.GetAccountBalancesAsync(accountId: null, CancellationToken.None);

    private UndoImportRunUseCase NewUseCase() =>
        new(_runs, _transactions, _transfers, _accounts, _categories, _importQueries, _unitOfWork);

    private Task UndoAsync(Guid importRunId) => NewUseCase().ExecuteAsync(importRunId, CancellationToken.None);
}
