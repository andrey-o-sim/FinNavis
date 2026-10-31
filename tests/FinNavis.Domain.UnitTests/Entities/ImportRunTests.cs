using System.Reflection;

using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Domain.UnitTests.Entities;

public sealed class ImportRunTests
{
    private const string AlreadyUndone = "Import.AlreadyUndone";

    private static readonly DateTimeOffset CreatedAt =
        new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    private static readonly DateOnly OccurredOn = new(2026, 2, 28);

    [Fact]
    public void Record_Statement_StartsCompletedWithNothingCounted()
    {
        Guid id = Guid.CreateVersion7();
        Guid accountId = Guid.CreateVersion7();

        ImportRun run = ImportRun.Record(
            id, "test-statement.csv", ImportRunKind.Statement, accountId, CreatedAt);

        run.Id.Should().Be(id);
        run.FileName.Should().Be("test-statement.csv");
        run.Kind.Should().Be(ImportRunKind.Statement);
        run.Status.Should().Be(ImportRunStatus.Completed);
        run.AccountId.Should().Be(accountId);
        run.RecordedRowCount.Should().Be(0);
        run.AlreadyPresentRowCount.Should().Be(0);
        run.CreatedAt.Should().Be(CreatedAt);
        run.TransactionLinks.Should().BeEmpty();
        run.TransferLinks.Should().BeEmpty();
        run.CreatedAccountLinks.Should().BeEmpty();
        run.CreatedCategoryLinks.Should().BeEmpty();
        run.SetAsideRows.Should().BeEmpty();
    }

    [Fact]
    public void Record_HistoryMigrationWithoutAccount_IsAccepted()
    {
        ImportRun run = HistoryMigration();

        run.Kind.Should().Be(ImportRunKind.HistoryMigration);
        run.AccountId.Should().BeNull();
    }

    [Fact]
    public void Record_HistoryMigrationNamingAnAccount_IsRefused()
    {
        Func<ImportRun> act = () => ImportRun.Record(
            Guid.CreateVersion7(),
            "test-export.csv",
            ImportRunKind.HistoryMigration,
            Guid.CreateVersion7(),
            CreatedAt);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Record_StatementWithoutAccount_IsRefused()
    {
        Func<ImportRun> act = () => ImportRun.Record(
            Guid.CreateVersion7(), "test-statement.csv", ImportRunKind.Statement, null, CreatedAt);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Record_BlankFileName_IsRefused(string fileName)
    {
        Func<ImportRun> act = () => ImportRun.Record(
            Guid.CreateVersion7(), fileName, ImportRunKind.HistoryMigration, null, CreatedAt);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Record_FileNameOverTheBound_IsRefused()
    {
        string fileName = new('a', ImportRun.MaxFileNameLength + 1);

        Func<ImportRun> act = () => ImportRun.Record(
            Guid.CreateVersion7(), fileName, ImportRunKind.HistoryMigration, null, CreatedAt);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Record_FileNameAtTheBound_IsAccepted()
    {
        string fileName = new('a', ImportRun.MaxFileNameLength);

        ImportRun run = ImportRun.Record(
            Guid.CreateVersion7(), fileName, ImportRunKind.HistoryMigration, null, CreatedAt);

        run.FileName.Should().Be(fileName);
    }

    [Fact]
    public void Counts_RecordedAndAlreadyPresentRows_ReadBackBoth()
    {
        ImportRun run = HistoryMigration();

        run.LinkRecordedTransaction(Guid.CreateVersion7(), RecordTransaction());
        run.LinkRecordedTransaction(Guid.CreateVersion7(), RecordTransaction());
        run.LinkRecordedTransfer(Guid.CreateVersion7(), RecordTransfer());
        run.CountAlreadyPresentRow();
        run.CountAlreadyPresentRow();

        run.RecordedRowCount.Should().Be(3);
        run.AlreadyPresentRowCount.Should().Be(2);
    }

    [Fact]
    public void SetAside_EveryField_ReadsBackOnTheRun()
    {
        ImportRun run = Statement();
        Guid rowId = Guid.CreateVersion7();

        run.SetAside(
            rowId, 7, SetAsideReason.NoRuleCovers, "Test Category", "2026-02-28;Test Shop;-12.50");

        SetAsideRow row = run.SetAsideRows.Should().ContainSingle().Which;
        row.Id.Should().Be(rowId);
        row.ImportRunId.Should().Be(run.Id);
        row.RowNumber.Should().Be(7);
        row.Reason.Should().Be(SetAsideReason.NoRuleCovers);
        row.SourceCategory.Should().Be("Test Category");
        row.RawLine.Should().Be("2026-02-28;Test Shop;-12.50");
        row.CreatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void SetAside_NoSourceCategory_KeepsItNull()
    {
        ImportRun run = Statement();

        run.SetAside(
            Guid.CreateVersion7(), 2, SetAsideReason.ForeignCurrency, null, "2026-02-28;-5.00;EUR");

        run.SetAsideRows.Single().SourceCategory.Should().BeNull();
    }

    [Fact]
    public void SetAside_ManyRows_DoesNotCountThemAsRecordedOrAlreadyPresent()
    {
        ImportRun run = Statement();

        run.SetAside(Guid.CreateVersion7(), 1, SetAsideReason.NoRuleCovers, null, "line one");
        run.SetAside(
            Guid.CreateVersion7(), 2, SetAsideReason.TransferBetweenOwnAccounts, null, "line two");

        run.SetAsideRows.Select(row => row.RowNumber).Should().Equal(1, 2);
        run.RecordedRowCount.Should().Be(0);
        run.AlreadyPresentRowCount.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetAside_RowNumberBelowOne_IsRefused(int rowNumber)
    {
        ImportRun run = Statement();

        Action act = () => run.SetAside(
            Guid.CreateVersion7(), rowNumber, SetAsideReason.NoRuleCovers, null, "line");

        act.Should().Throw<ArgumentOutOfRangeException>();
        run.SetAsideRows.Should().BeEmpty();
    }

    [Fact]
    public void SetAside_RawLineOverTheBound_IsRefused()
    {
        ImportRun run = Statement();
        string rawLine = new('x', SetAsideRow.MaxRawLineLength + 1);

        Action act = () => run.SetAside(
            Guid.CreateVersion7(), 1, SetAsideReason.NoRuleCovers, null, rawLine);

        act.Should().Throw<ArgumentOutOfRangeException>();
        run.SetAsideRows.Should().BeEmpty();
    }

    [Fact]
    public void SetAside_SourceCategoryOverTheBound_IsRefused()
    {
        ImportRun run = Statement();
        string sourceCategory = new('c', SetAsideRow.MaxSourceCategoryLength + 1);

        Action act = () => run.SetAside(
            Guid.CreateVersion7(), 1, SetAsideReason.NoRuleCovers, sourceCategory, "line");

        act.Should().Throw<ArgumentOutOfRangeException>();
        run.SetAsideRows.Should().BeEmpty();
    }

    [Fact]
    public void SetAsideRow_HasNoStateAndNoTransition()
    {
        // A set-aside row is never recorded on its own: the type offers no method to act on it,
        // no status to move through and no reference to a ledger movement.
        Type type = typeof(SetAsideRow);

        type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Should().BeEmpty();

        type.GetProperties()
            .Should().OnlyContain(property => property.SetMethod == null || !property.SetMethod.IsPublic);

        type.GetProperties().Select(property => property.Name).Should().BeEquivalentTo(
            "Id",
            "ImportRunId",
            "RowNumber",
            "Reason",
            "SourceCategory",
            "RawLine",
            "CreatedAt");

        type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Should().BeEmpty();
    }

    [Fact]
    public void LinkRecordedTransaction_UnchangedTransaction_StillMatches()
    {
        ImportRun run = Statement();
        Transaction transaction = RecordTransaction();

        run.LinkRecordedTransaction(Guid.CreateVersion7(), transaction);

        ImportTransactionLink link = run.TransactionLinks.Should().ContainSingle().Which;
        link.ImportRunId.Should().Be(run.Id);
        link.TransactionId.Should().Be(transaction.Id);
        link.Fingerprint.Should().Be(TransactionFingerprint(transaction));
        link.CreatedAt.Should().Be(CreatedAt);
        link.StillMatches(transaction).Should().BeTrue();
    }

    [Fact]
    public void StillMatches_TransactionEditedAfterTheLoad_ReportsAMismatch()
    {
        ImportRun run = Statement();
        Transaction transaction = RecordTransaction();
        run.LinkRecordedTransaction(Guid.CreateVersion7(), transaction);

        transaction.Edit(
            transaction.AccountId,
            secondAccountId: null,
            transaction.CategoryId,
            new Money(13.0000m),
            transaction.OccurredOn,
            transaction.Note);

        run.TransactionLinks.Single().StillMatches(transaction).Should().BeFalse();
    }

    [Fact]
    public void StillMatches_TransactionNoteEditedAfterTheLoad_ReportsAMismatch()
    {
        ImportRun run = Statement();
        Transaction transaction = RecordTransaction();
        run.LinkRecordedTransaction(Guid.CreateVersion7(), transaction);

        transaction.Edit(
            transaction.AccountId,
            secondAccountId: null,
            transaction.CategoryId,
            transaction.Amount,
            transaction.OccurredOn,
            "Edited by hand");

        run.TransactionLinks.Single().StillMatches(transaction).Should().BeFalse();
    }

    [Fact]
    public void StillMatches_ADifferentTransaction_IsRefused()
    {
        ImportRun run = Statement();
        run.LinkRecordedTransaction(Guid.CreateVersion7(), RecordTransaction());

        Func<bool> act = () => run.TransactionLinks.Single().StillMatches(RecordTransaction());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LinkRecordedTransfer_UnchangedTransfer_StillMatches()
    {
        ImportRun run = HistoryMigration();
        Transfer transfer = RecordTransfer();

        run.LinkRecordedTransfer(Guid.CreateVersion7(), transfer);

        ImportTransferLink link = run.TransferLinks.Should().ContainSingle().Which;
        link.ImportRunId.Should().Be(run.Id);
        link.TransferId.Should().Be(transfer.Id);
        link.Fingerprint.Should().Be(TransferFingerprint(transfer));
        link.CreatedAt.Should().Be(CreatedAt);
        link.StillMatches(transfer).Should().BeTrue();
    }

    [Fact]
    public void StillMatches_TransferEditedAfterTheLoad_ReportsAMismatch()
    {
        ImportRun run = HistoryMigration();
        Transfer transfer = RecordTransfer();
        run.LinkRecordedTransfer(Guid.CreateVersion7(), transfer);

        transfer.Edit(
            transfer.SourceAccountId,
            transfer.DestinationAccountId,
            transfer.Amount,
            OccurredOn.AddDays(1));

        run.TransferLinks.Single().StillMatches(transfer).Should().BeFalse();
    }

    [Fact]
    public void StillMatches_ADifferentTransfer_IsRefused()
    {
        ImportRun run = HistoryMigration();
        run.LinkRecordedTransfer(Guid.CreateVersion7(), RecordTransfer());

        Func<bool> act = () => run.TransferLinks.Single().StillMatches(RecordTransfer());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LinkCreatedAccount_AccountTheRunMade_IsRecordedOnTheRun()
    {
        ImportRun run = HistoryMigration();
        Account created = Account.Create(Guid.CreateVersion7(), "Test Account", CreatedAt);
        Guid linkId = Guid.CreateVersion7();

        run.LinkCreatedAccount(linkId, created);

        // Undo tells an account the run made from one the owner made by this link alone.
        ImportCreatedAccountLink link = run.CreatedAccountLinks.Should().ContainSingle().Which;
        link.Id.Should().Be(linkId);
        link.ImportRunId.Should().Be(run.Id);
        link.AccountId.Should().Be(created.Id);
        link.CreatedAt.Should().Be(CreatedAt);
        run.RecordedRowCount.Should().Be(0);
    }

    [Fact]
    public void LinkCreatedCategory_CategoryTheRunMade_IsRecordedOnTheRun()
    {
        ImportRun run = HistoryMigration();
        Category created = Category.Create(
            Guid.CreateVersion7(), "Test Category", CategoryKind.Expense, group: null, CreatedAt);
        Guid linkId = Guid.CreateVersion7();

        run.LinkCreatedCategory(linkId, created);

        ImportCreatedCategoryLink link = run.CreatedCategoryLinks.Should().ContainSingle().Which;
        link.Id.Should().Be(linkId);
        link.ImportRunId.Should().Be(run.Id);
        link.CategoryId.Should().Be(created.Id);
        link.CreatedAt.Should().Be(CreatedAt);
        run.RecordedRowCount.Should().Be(0);
    }

    [Fact]
    public void MarkUndone_CompletedRun_BecomesUndoneAndKeepsWhatItDid()
    {
        ImportRun run = Statement();
        run.LinkRecordedTransaction(Guid.CreateVersion7(), RecordTransaction());
        run.CountAlreadyPresentRow();
        run.SetAside(Guid.CreateVersion7(), 3, SetAsideReason.NoRuleCovers, null, "line");

        run.MarkUndone();

        run.Status.Should().Be(ImportRunStatus.Undone);
        run.RecordedRowCount.Should().Be(1);
        run.AlreadyPresentRowCount.Should().Be(1);
        run.SetAsideRows.Should().ContainSingle();
    }

    [Fact]
    public void MarkUndone_RunAlreadyUndone_IsRefusedAndTheRunIsUnchanged()
    {
        ImportRun run = Statement();
        run.LinkRecordedTransaction(Guid.CreateVersion7(), RecordTransaction());
        run.CountAlreadyPresentRow();
        run.MarkUndone();

        Action act = run.MarkUndone;

        DomainRuleViolationException refusal = act.Should()
            .Throw<DomainRuleViolationException>().Which;
        refusal.Code.Should().Be(AlreadyUndone);
        refusal.Message.Should().Be(
            "This load was already undone, so there is nothing left to remove.");
        run.Status.Should().Be(ImportRunStatus.Undone);
        run.RecordedRowCount.Should().Be(1);
        run.AlreadyPresentRowCount.Should().Be(1);
        run.TransactionLinks.Should().ContainSingle();
    }

    [Fact]
    public void ImportRunStatus_HasNoFailedValue()
    {
        // The whole run commits in one database transaction, so a load that fails leaves no run.
        Enum.GetNames<ImportRunStatus>().Should().BeEquivalentTo("Completed", "Undone");
    }

    [Fact]
    public void ImportRun_CarriesNoUndoneMomentAndNoMappingProfile()
    {
        IEnumerable<string> propertyNames = typeof(ImportRun).GetProperties()
            .Select(property => property.Name);

        propertyNames.Should().NotContain("UndoneAt");
        propertyNames.Should().NotContain(
            name => name.Contains("MappingProfile", StringComparison.Ordinal));
    }

    private static ImportRun Statement() =>
        ImportRun.Record(
            Guid.CreateVersion7(),
            "test-statement.csv",
            ImportRunKind.Statement,
            Guid.CreateVersion7(),
            CreatedAt);

    private static ImportRun HistoryMigration() =>
        ImportRun.Record(
            Guid.CreateVersion7(), "test-export.csv", ImportRunKind.HistoryMigration, null, CreatedAt);

    private static Transaction RecordTransaction() =>
        Transaction.Record(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new Money(12.5000m),
            OccurredOn,
            "Test Shop",
            CreatedAt);

    private static Transfer RecordTransfer() =>
        Transfer.Record(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new Money(180.0000m),
            OccurredOn,
            CreatedAt);

    private static string TransactionFingerprint(Transaction transaction) =>
        MovementFingerprint.OfTransaction(
            transaction.AccountId,
            transaction.CategoryId,
            transaction.Amount,
            transaction.OccurredOn,
            transaction.Note).Value;

    private static string TransferFingerprint(Transfer transfer) =>
        MovementFingerprint.OfTransfer(
            transfer.SourceAccountId,
            transfer.DestinationAccountId,
            transfer.Amount,
            transfer.OccurredOn).Value;
}
