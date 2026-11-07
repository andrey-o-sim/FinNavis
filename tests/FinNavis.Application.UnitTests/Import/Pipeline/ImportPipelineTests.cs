using System.Collections.ObjectModel;
using System.Globalization;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Application.Import.Pipeline;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.UnitTests.Import.Pipeline;

public sealed class ImportPipelineTests
{
    private const string FileName = "test-statement-2026-10.csv";
    private const string GroceriesInFile = "Test groceries";
    private const string CafeInFile = "Test cafe";
    private const int StatementWindowDays = 1;
    private const int HistoryWindowDays = 0;

    private static readonly DateTimeOffset LoadedAt = new(2026, 11, 7, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly StatementDay = new(2026, 10, 15);

    private readonly FakeImportRunRepository _runs = new();
    private readonly FakeTransactionRepository _transactions = new();
    private readonly FakeTransferRepository _transfers = new();
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeImportQueries _importQueries;
    private readonly FakeUnitOfWork _unitOfWork;
    private readonly Account _wallet;
    private readonly Category _groceries;
    private readonly Category _cafe;

    public ImportPipelineTests()
    {
        _importQueries = new FakeImportQueries(_runs, _transactions, _transfers, _categories);
        _unitOfWork = new FakeUnitOfWork(() =>
        {
            _transactions.Commit();
            _transfers.Commit();
            _categories.Commit();
        });

        _wallet = Account.Create(Guid.CreateVersion7(), "Test wallet", LoadedAt.AddDays(-30));
        _groceries = SeedCategory("Test groceries");
        _cafe = SeedCategory("Test cafe");
    }

    [Fact]
    public async Task RunAsync_SameFileHandedOverTwice_RecordsNothingTheSecondTimeAndCountsEveryRowAlreadyThere()
    {
        StatementRow[] file =
        [
            Row(2, StatementDay, 12.5000m, GroceriesInFile),
            Row(3, StatementDay.AddDays(3), 40.0000m, GroceriesInFile),
        ];
        await LoadAsync(StatementPolicy(), file);

        ImportRun second = await LoadAsync(StatementPolicy(), file);

        second.RecordedRowCount.Should().Be(0);
        second.AlreadyPresentRowCount.Should().Be(2);
        second.TransactionLinks.Should().BeEmpty();
        second.SetAsideRows.Should().BeEmpty();
        _transactions.Stored.Should().HaveCount(2);
    }

    // A movement typed by hand carries no external reference, so only the date-and-amount match can
    // find it. The bank's text and the owner's note almost never agree, so the text must not count.
    [Fact]
    public async Task RunAsync_RowMatchesMovementTypedByHand_CountsItAlreadyThereAndDoesNotSetItAside()
    {
        Transaction typedByHand = SeedTypedByHand(StatementDay, 12.5000m, note: "milk and bread");

        ImportRun run = await LoadAsync(
            StatementPolicy(),
            Row(2, StatementDay, 12.5000m, GroceriesInFile, note: "TEST SHOP TEST CITY"));

        typedByHand.ExternalRef.Should().BeNull();
        run.AlreadyPresentRowCount.Should().Be(1);
        run.RecordedRowCount.Should().Be(0);
        run.SetAsideRows.Should().BeEmpty();
        _transactions.Stored.Should().ContainSingle().Which.Id.Should().Be(typedByHand.Id);
    }

    // Two real purchases of the same thing on the same day are both spending. If the match could see
    // what this run already recorded, the second purchase would be dropped with nothing shown to the
    // owner — so the snapshot is taken once, before the first write, and the run commits once.
    [Fact]
    public async Task RunAsync_TwoIdenticalRowsInOneFile_RecordsBoth()
    {
        ImportRun run = await LoadAsync(
            StatementPolicy(),
            Row(2, StatementDay, 3.2000m, GroceriesInFile),
            Row(3, StatementDay, 3.2000m, GroceriesInFile));

        run.RecordedRowCount.Should().Be(2);
        run.AlreadyPresentRowCount.Should().Be(0);
        _transactions.Stored.Should().HaveCount(2).And.OnlyContain(transaction =>
            transaction.AccountId == _wallet.Id
            && transaction.CategoryId == _groceries.Id
            && transaction.Amount == new Money(3.2000m)
            && transaction.OccurredOn == StatementDay
            && transaction.Note == "TEST SHOP");
        _importQueries.SnapshotsTaken.Should().Be(1);
        _unitOfWork.Commits.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_RowSetAsideByEarlierLoadNowCovered_RecordsItAndDoesNotRecordTheEarlierRowsAgain()
    {
        StatementRow groceriesRow = Row(2, StatementDay, 12.5000m, GroceriesInFile);
        StatementRow cafeRow = Row(3, StatementDay.AddDays(2), 4.8000m, CafeInFile);

        ImportRun first = await LoadAsync(StatementPolicy(coverCafe: false), groceriesRow, cafeRow);

        first.RecordedRowCount.Should().Be(1);
        SetAsideRow waiting = first.SetAsideRows.Should().ContainSingle().Subject;
        waiting.Reason.Should().Be(SetAsideReason.NoRuleCovers);
        waiting.SourceCategory.Should().Be(CafeInFile);
        waiting.RowNumber.Should().Be(cafeRow.LineNumber);
        waiting.RawLine.Should().Be(cafeRow.RawLine);

        ImportRun second = await LoadAsync(StatementPolicy(coverCafe: true), groceriesRow, cafeRow);

        second.RecordedRowCount.Should().Be(1);
        second.AlreadyPresentRowCount.Should().Be(1);
        second.SetAsideRows.Should().BeEmpty();
        _transactions.Stored.Should().HaveCount(2);
        _transactions.Stored.Should().ContainSingle(transaction => transaction.CategoryId == _groceries.Id);
        _transactions.Stored.Should().ContainSingle(transaction =>
            transaction.CategoryId == _cafe.Id && transaction.OccurredOn == StatementDay.AddDays(2));
    }

    // A late-evening purchase can carry the next day's date in the statement, because the bank
    // writes its own time zone, so a statement row matches a movement one day either side.
    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public async Task RunAsync_StatementRowOneDayFromMovementTypedByHand_CountsItAlreadyThere(int daysApart)
    {
        SeedTypedByHand(StatementDay, 12.5000m);

        ImportRun run = await LoadAsync(
            StatementPolicy(),
            Row(2, StatementDay.AddDays(daysApart), 12.5000m, GroceriesInFile));

        run.AlreadyPresentRowCount.Should().Be(1);
        run.RecordedRowCount.Should().Be(0);
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(2)]
    public async Task RunAsync_StatementRowTwoDaysFromMovementTypedByHand_RecordsIt(int daysApart)
    {
        SeedTypedByHand(StatementDay, 12.5000m);

        ImportRun run = await LoadAsync(
            StatementPolicy(),
            Row(2, StatementDay.AddDays(daysApart), 12.5000m, GroceriesInFile));

        run.AlreadyPresentRowCount.Should().Be(0);
        run.RecordedRowCount.Should().Be(1);
    }

    // The old tracker's dates were typed by the owner, so they are the dates meant: no window, and
    // a narrower window gives fewer false matches.
    [Fact]
    public async Task RunAsync_HistoryRowOneDayFromMovementTypedByHand_RecordsIt()
    {
        SeedTypedByHand(StatementDay, 12.5000m);

        ImportRun run = await LoadAsync(
            HistoryPolicy(),
            Row(2, StatementDay.AddDays(1), 12.5000m, GroceriesInFile));

        run.AlreadyPresentRowCount.Should().Be(0);
        run.RecordedRowCount.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_HistoryRowOnTheDayOfMovementTypedByHand_CountsItAlreadyThere()
    {
        SeedTypedByHand(StatementDay, 12.5000m);

        ImportRun run = await LoadAsync(
            HistoryPolicy(),
            Row(2, StatementDay, 12.5000m, GroceriesInFile));

        run.AlreadyPresentRowCount.Should().Be(1);
        run.RecordedRowCount.Should().Be(0);
    }

    [Fact]
    public async Task RunAsync_SameDayAndAmountOnAnotherAccount_RecordsIt()
    {
        Guid otherAccountId = Guid.CreateVersion7();
        SeedTypedByHand(StatementDay, 12.5000m, accountId: otherAccountId);

        ImportRun run = await LoadAsync(
            StatementPolicy(),
            Row(2, StatementDay, 12.5000m, GroceriesInFile));

        run.AlreadyPresentRowCount.Should().Be(0);
        run.RecordedRowCount.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_SameDayButAnotherAmount_RecordsIt()
    {
        SeedTypedByHand(StatementDay, 12.5000m);

        ImportRun run = await LoadAsync(
            StatementPolicy(),
            Row(2, StatementDay, 12.5100m, GroceriesInFile));

        run.AlreadyPresentRowCount.Should().Be(0);
        run.RecordedRowCount.Should().Be(1);
    }

    // One movement in the ledger stands for one purchase. If it could cover two rows, the second
    // real purchase would vanish.
    [Fact]
    public async Task RunAsync_TwoIdenticalRowsAgainstOneMovementTypedByHand_CountsOneAlreadyThereAndRecordsTheOther()
    {
        SeedTypedByHand(StatementDay, 3.2000m);

        ImportRun run = await LoadAsync(
            StatementPolicy(),
            Row(2, StatementDay, 3.2000m, GroceriesInFile),
            Row(3, StatementDay, 3.2000m, GroceriesInFile));

        run.AlreadyPresentRowCount.Should().Be(1);
        run.RecordedRowCount.Should().Be(1);
        _transactions.Stored.Should().HaveCount(2);
    }

    // The farther movement is seeded first, so a match that takes the first candidate it finds
    // uses it up and leaves the second row with nothing, which records it.
    [Fact]
    public async Task RunAsync_RowMatchesTwoMovements_TakesTheOneWithTheClosestDate()
    {
        SeedTypedByHand(StatementDay, 12.5000m);
        SeedTypedByHand(StatementDay.AddDays(1), 12.5000m);

        ImportRun run = await LoadAsync(
            StatementPolicy(),
            Row(2, StatementDay.AddDays(1), 12.5000m, GroceriesInFile),
            Row(3, StatementDay.AddDays(-1), 12.5000m, GroceriesInFile));

        run.AlreadyPresentRowCount.Should().Be(2);
        run.RecordedRowCount.Should().Be(0);
    }

    // The ids are written out rather than generated, because two ids made in the same millisecond
    // have no guaranteed order. The larger id is seeded first, so a match that takes the first
    // candidate it finds picks the wrong one.
    [Fact]
    public async Task RunAsync_RowMatchesTwoMovementsEquallyClose_TakesTheOneWithTheSmallestId()
    {
        Guid smallerId = Guid.Parse("00000000-0000-7000-8000-000000000001");
        Guid largerId = Guid.Parse("00000000-0000-7000-8000-000000000002");
        SeedTypedByHand(StatementDay.AddDays(1), 12.5000m, id: largerId);
        SeedTypedByHand(StatementDay.AddDays(-1), 12.5000m, id: smallerId);

        ImportRun run = await LoadAsync(
            StatementPolicy(),
            Row(2, StatementDay, 12.5000m, GroceriesInFile),
            Row(3, StatementDay.AddDays(-2), 12.5000m, GroceriesInFile));

        run.AlreadyPresentRowCount.Should().Be(1);
        run.RecordedRowCount.Should().Be(1);
        _transactions.Stored.Should().ContainSingle(transaction =>
            transaction.Id != smallerId && transaction.Id != largerId)
            .Which.OccurredOn.Should().Be(StatementDay.AddDays(-2));
    }

    // Undo compares this stored hash with the movement as it is then, to see whether the owner
    // edited it since the load.
    [Fact]
    public async Task RunAsync_RecordedRows_EachGetALinkCarryingItsFingerprint()
    {
        ImportRun run = await LoadAsync(
            StatementPolicy(),
            Row(2, StatementDay, 12.5000m, GroceriesInFile),
            Row(3, StatementDay.AddDays(2), 4.8000m, CafeInFile, note: "TEST COFFEE"));

        run.TransactionLinks.Should().HaveCount(2);
        run.TransactionLinks.Select(link => link.TransactionId)
            .Should().BeEquivalentTo(_transactions.Stored.Select(transaction => transaction.Id));

        foreach (Transaction transaction in _transactions.Stored)
        {
            ImportTransactionLink link =
                run.TransactionLinks.Single(candidate => candidate.TransactionId == transaction.Id);
            link.ImportRunId.Should().Be(run.Id);
            link.Fingerprint.Should().Be(MovementFingerprint.OfTransaction(
                transaction.AccountId,
                transaction.CategoryId,
                transaction.Amount,
                transaction.OccurredOn,
                transaction.Note).Value);
        }
    }

    [Fact]
    public async Task RunAsync_SeveralRows_AddsOneRunAndCommitsOnce()
    {
        ImportRun run = await LoadAsync(
            StatementPolicy(coverCafe: false),
            Row(2, StatementDay, 12.5000m, GroceriesInFile),
            Row(3, StatementDay.AddDays(1), 4.8000m, CafeInFile),
            Row(4, StatementDay.AddDays(2), 20.0000m, GroceriesInFile),
            Row(5, StatementDay.AddDays(3), 7.1000m, GroceriesInFile));

        _unitOfWork.Commits.Should().Be(1);
        _runs.Staged.Should().ContainSingle().Which.Should().BeSameAs(run);
        run.Kind.Should().Be(ImportRunKind.Statement);
        run.AccountId.Should().Be(_wallet.Id);
        run.FileName.Should().Be(FileName);
        run.CreatedAt.Should().Be(LoadedAt);
        run.RecordedRowCount.Should().Be(3);
        run.SetAsideRows.Should().ContainSingle();
    }

    private static StatementRow Row(
        int lineNumber,
        DateOnly day,
        decimal amount,
        string category,
        string note = "TEST SHOP")
    {
        string[] values =
        [
            day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            amount.ToString("F4", CultureInfo.InvariantCulture),
            category,
            note,
        ];

        return new StatementRow(lineNumber, values, string.Join(';', values));
    }

    private static StatementHeader Header()
    {
        HashSet<StatementColumn> missing = [];

        return new StatementHeader(
            ["date", "amount", "category", "note"],
            "date;amount;category;note",
            missing,
            ReadOnlyDictionary<StatementColumn, int>.Empty);
    }

    private FakeImportPolicy StatementPolicy(bool coverCafe = true) =>
        Covering(new FakeImportPolicy(ImportRunKind.Statement, _wallet.Id, StatementWindowDays), coverCafe);

    private FakeImportPolicy HistoryPolicy() =>
        Covering(new FakeImportPolicy(ImportRunKind.HistoryMigration, _wallet.Id, HistoryWindowDays), coverCafe: true);

    private FakeImportPolicy Covering(FakeImportPolicy policy, bool coverCafe)
    {
        policy.Cover(GroceriesInFile, _groceries.Id);

        return coverCafe ? policy.Cover(CafeInFile, _cafe.Id) : policy;
    }

    private async Task<ImportRun> LoadAsync(FakeImportPolicy policy, params StatementRow[] rows)
    {
        ImportPipeline pipeline = new(
            new FakeStatementReader(Header(), rows),
            _importQueries,
            _transactions,
            _runs,
            _unitOfWork,
            new FixedClock(LoadedAt));

        return await pipeline.RunAsync(Stream.Null, FileName, policy, CancellationToken.None);
    }

    private Category SeedCategory(string name)
    {
        Category category = Category.Create(
            Guid.CreateVersion7(), name, CategoryKind.Expense, group: null, LoadedAt.AddDays(-30));
        _categories.Seed(category);

        return category;
    }

    private Transaction SeedTypedByHand(
        DateOnly day,
        decimal amount,
        string? note = null,
        Guid? accountId = null,
        Guid? id = null)
    {
        Transaction transaction = Transaction.Record(
            id ?? Guid.CreateVersion7(),
            accountId ?? _wallet.Id,
            _groceries.Id,
            new Money(amount),
            day,
            note,
            LoadedAt.AddDays(-20));
        _transactions.Seed(transaction);

        return transaction;
    }
}
