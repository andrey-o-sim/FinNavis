using System.Reflection;
using FinNavis.Application.Import.GetImportRun;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;
using GetImportRunUseCase = FinNavis.Application.Import.GetImportRun.GetImportRun;

namespace FinNavis.Application.UnitTests.Import.GetImportRun;

public sealed class GetImportRunTests
{
    private const string FileName = "test-statement-2026-10.csv";

    private static readonly DateTimeOffset LoadedAt = new(2026, 11, 7, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeImportRunRepository _runs = new();

    [Fact]
    public async Task ExecuteAsync_FinishedRun_ReportsBothCountsAndTheRunItself()
    {
        Guid accountId = Guid.CreateVersion7();
        ImportRun run = StatementRun(accountId);
        RecordTransaction(run, accountId);
        RecordTransaction(run, accountId);
        run.CountAlreadyPresentRow();
        run.CountAlreadyPresentRow();
        run.CountAlreadyPresentRow();
        _runs.Seed(run);

        ImportRunReport report = await ReadAsync(run.Id);

        report.ImportRunId.Should().Be(run.Id);
        report.Kind.Should().Be(ImportRunKind.Statement);
        report.FileName.Should().Be(FileName);
        report.AccountId.Should().Be(accountId);
        report.CreatedAt.Should().Be(LoadedAt);
        report.RecordedRowCount.Should().Be(2);
        report.AlreadyPresentRowCount.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_RunWithSetAsideRows_ListsEachRowWithItsReasonNumberCategoryAndLine()
    {
        ImportRun run = StatementRun(Guid.CreateVersion7());
        run.SetAside(
            Guid.CreateVersion7(), 4, SetAsideReason.NoRuleCovers, "TEST HOBBIES", "2026-10-04,TEST SHOP,TEST HOBBIES,-9.9000");
        run.SetAside(
            Guid.CreateVersion7(), 7, SetAsideReason.ForeignCurrency, null, "2026-10-07,TEST ABROAD,,-15.0000");
        _runs.Seed(run);

        ImportRunReport report = await ReadAsync(run.Id);

        report.SetAside.Should().BeEquivalentTo(
            [
                new ReportedSetAsideRow(
                    4, SetAsideReason.NoRuleCovers, "TEST HOBBIES", "2026-10-04,TEST SHOP,TEST HOBBIES,-9.9000"),
                new ReportedSetAsideRow(
                    7, SetAsideReason.ForeignCurrency, null, "2026-10-07,TEST ABROAD,,-15.0000"),
            ],
            options => options.WithStrictOrdering());
    }

    // The endpoint pages the list by a cursor over row_number, so the list it receives must already
    // be in file order, whatever order the rows were stored in.
    [Fact]
    public async Task ExecuteAsync_SetAsideRowsStoredOutOfOrder_ReturnsThemOrderedByRowNumber()
    {
        ImportRun run = StatementRun(Guid.CreateVersion7());
        run.SetAside(Guid.CreateVersion7(), 12, SetAsideReason.NoRuleCovers, "TEST C", "line 12");
        run.SetAside(Guid.CreateVersion7(), 3, SetAsideReason.NoRuleCovers, "TEST A", "line 3");
        run.SetAside(Guid.CreateVersion7(), 8, SetAsideReason.TransferBetweenOwnAccounts, "TEST B", "line 8");
        _runs.Seed(run);

        ImportRunReport report = await ReadAsync(run.Id);

        report.SetAside.Select(row => row.RowNumber).Should().Equal(3, 8, 12);
    }

    [Fact]
    public async Task ExecuteAsync_RunThatSetNothingAside_ReportsAnEmptyList()
    {
        Guid accountId = Guid.CreateVersion7();
        ImportRun run = StatementRun(accountId);
        RecordTransaction(run, accountId);
        _runs.Seed(run);

        ImportRunReport report = await ReadAsync(run.Id);

        report.SetAside.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_RunNotUndone_ReportsItAsCompleted()
    {
        ImportRun run = StatementRun(Guid.CreateVersion7());
        _runs.Seed(run);

        ImportRunReport report = await ReadAsync(run.Id);

        report.Status.Should().Be(ImportRunStatus.Completed);
    }

    // Undo removes the links, but the report is a record of what the load did, so the stored
    // counts and the set-aside rows must still be there next to the Undone status.
    [Fact]
    public async Task ExecuteAsync_UndoneRun_ReportsItAsUndoneAndKeepsWhatTheLoadDid()
    {
        Guid accountId = Guid.CreateVersion7();
        ImportRun run = StatementRun(accountId);
        RecordTransaction(run, accountId);
        run.CountAlreadyPresentRow();
        run.SetAside(Guid.CreateVersion7(), 5, SetAsideReason.NoRuleCovers, "TEST HOBBIES", "line 5");
        run.MarkUndone();
        _runs.Seed(run);

        ImportRunReport report = await ReadAsync(run.Id);

        report.Status.Should().Be(ImportRunStatus.Undone);
        report.RecordedRowCount.Should().Be(1);
        report.AlreadyPresentRowCount.Should().Be(1);
        report.SetAside.Should().ContainSingle().Which.RowNumber.Should().Be(5);
    }

    [Fact]
    public async Task ExecuteAsync_HistoryMigrationRun_ReportsNoAccount()
    {
        ImportRun run = ImportRun.Record(
            Guid.CreateVersion7(), "test-history.csv", ImportRunKind.HistoryMigration, accountId: null, LoadedAt);
        _runs.Seed(run);

        ImportRunReport report = await ReadAsync(run.Id);

        report.Kind.Should().Be(ImportRunKind.HistoryMigration);
        report.AccountId.Should().BeNull();
    }

    // A set-aside row waits for the owner. Reading the report is the owner doing nothing, so it
    // must not record the row, stage a change, or stop listing it.
    [Fact]
    public async Task ExecuteAsync_RunWithSetAsideRows_RecordsNothingAndKeepsTheRowsSetAside()
    {
        ImportRun run = StatementRun(Guid.CreateVersion7());
        run.SetAside(Guid.CreateVersion7(), 4, SetAsideReason.NoRuleCovers, "TEST HOBBIES", "line 4");
        _runs.Seed(run);

        ImportRunReport first = await ReadAsync(run.Id);
        ImportRunReport second = await ReadAsync(run.Id);

        first.RecordedRowCount.Should().Be(0);
        second.SetAside.Should().ContainSingle().Which.RowNumber.Should().Be(4);
        run.SetAsideRows.Should().ContainSingle();
        run.TransactionLinks.Should().BeEmpty();
        run.TransferLinks.Should().BeEmpty();
        run.RecordedRowCount.Should().Be(0);
        _runs.Staged.Should().BeEmpty();
    }

    // Without a transaction repository, a transfer repository, a unit of work or the import
    // queries, the use case has no way to record a movement or to read across the ledger.
    [Fact]
    public void Constructor_Dependencies_AreTheImportRunRepositoryOnly()
    {
        ConstructorInfo constructor =
            typeof(GetImportRunUseCase).GetConstructors().Should().ContainSingle().Subject;

        constructor.GetParameters().Select(parameter => parameter.ParameterType)
            .Should().Equal(typeof(IImportRunRepository));
    }

    [Fact]
    public void ReportedSetAsideRow_Shape_CarriesNoReferenceIntoTheLedger()
    {
        typeof(ReportedSetAsideRow).GetProperties().Select(property => property.Name)
            .Should().BeEquivalentTo(
                [
                    nameof(ReportedSetAsideRow.RowNumber),
                    nameof(ReportedSetAsideRow.Reason),
                    nameof(ReportedSetAsideRow.SourceCategory),
                    nameof(ReportedSetAsideRow.RawLine),
                ]);
    }

    [Fact]
    public async Task ExecuteAsync_KnownRun_ReadsItThroughTheRepositoryPassingTheTokenThrough()
    {
        ImportRun run = StatementRun(Guid.CreateVersion7());
        _runs.Seed(run);
        using var cancellation = new CancellationTokenSource();

        await new GetImportRunUseCase(_runs).ExecuteAsync(run.Id, cancellation.Token);

        _runs.RequestedIds.Should().Equal(run.Id);
        _runs.TokensSeen.Should().Equal(cancellation.Token);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownRunId_IsRefusedAsImportNotFound()
    {
        _runs.Seed(StatementRun(Guid.CreateVersion7()));
        Guid unknownId = Guid.CreateVersion7();

        Func<Task> read = () => ReadAsync(unknownId);

        DomainRuleViolationException refusal =
            (await read.Should().ThrowAsync<DomainRuleViolationException>()).Which;
        refusal.Code.Should().Be("Import.NotFound");
        refusal.Message.Should().Be($"No load in this installation has id {unknownId}.");
        _runs.Staged.Should().BeEmpty();
    }

    private static ImportRun StatementRun(Guid accountId) =>
        ImportRun.Record(Guid.CreateVersion7(), FileName, ImportRunKind.Statement, accountId, LoadedAt);

    private static void RecordTransaction(ImportRun run, Guid accountId)
    {
        Transaction transaction = Transaction.Record(
            Guid.CreateVersion7(),
            accountId,
            Guid.CreateVersion7(),
            new Money(12.5m),
            new DateOnly(2026, 10, 1),
            note: null,
            LoadedAt);

        run.LinkRecordedTransaction(Guid.CreateVersion7(), transaction);
    }

    private Task<ImportRunReport> ReadAsync(Guid importRunId) =>
        new GetImportRunUseCase(_runs).ExecuteAsync(importRunId, CancellationToken.None);
}
