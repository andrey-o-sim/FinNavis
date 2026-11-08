using System.Globalization;
using System.Runtime.CompilerServices;
using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Application.Common;
using FinNavis.Application.Import.Pipeline;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;
using LoadStatementUseCase = FinNavis.Application.Import.LoadStatement.LoadStatement;

namespace FinNavis.Application.UnitTests.Import.LoadStatement;

public sealed class LoadStatementTests
{
    private const string FileName = "test-statement-2026-10.csv";
    private const string LedgerCurrency = "EUR";
    private const string GroceriesInFile = "TEST GROCERIES";
    private const string CafeInFile = "TEST CAFE";
    private const string OwnCardInFile = "TEST OWN CARD";
    private const string BooksInFile = "TEST BOOKS";
    private const string FirstCard = "**** 0001";

    private const string UnknownAccountMessage =
        "You do not have an account with that name, so there was nothing to load this file onto. "
        + "Nothing was recorded and no account was created.";

    private const string NoRulesMessage =
        "There are no confirmed rules for this kind of file yet, so its rows had nowhere to go. "
        + "Nothing was recorded.";

    private const string NotRecordedMessage =
        "The load stopped before it finished, so nothing from it was recorded and every balance is unchanged.";

    private const string NotReadableMessage = "This file could not be read as a statement, so nothing was recorded.";

    private const string MissingColumnMessage =
        "This file does not say the date or the amount, so nothing can be recorded from it.";

    private const int DateColumn = 0;
    private const int CategoryColumn = 1;
    private const int CardColumn = 2;
    private const int CardAmountColumn = 4;
    private const int PurchaseAmountColumn = 6;
    private const int PurchaseCurrencyColumn = 7;

    private static readonly string[] ColumnNames =
    [
        "Дата",
        "Категорія",
        "Картка",
        "Опис операції",
        "Сума в валюті картки",
        "Валюта картки",
        "Сума в валюті транзакції",
        "Валюта транзакції",
        "Залишок на кінець періоду",
        "Валюта залишку",
    ];

    private static readonly string ShapeKey =
        string.Join('|', ColumnNames.Select(name => name.ToLowerInvariant()));

    private static readonly DateTimeOffset LoadedAt = new(2026, 11, 7, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly StatementDay = new(2026, 10, 15);

    private readonly FakeAccountRepository _accounts = new();
    private readonly FakeMappingProfileRepository _profiles = new();
    private readonly FakeImportRunRepository _runs = new();
    private readonly FakeTransactionRepository _transactions = new();
    private readonly FakeTransferRepository _transfers = new();
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeImportQueries _importQueries;
    private readonly FakeUnitOfWork _unitOfWork;
    private readonly Account _wallet;
    private readonly Category _groceries;
    private readonly Category _cafe;

    public LoadStatementTests()
    {
        _importQueries = new FakeImportQueries(_runs, _transactions, _transfers, _categories);
        _unitOfWork = new FakeUnitOfWork(CommitEverything);

        _wallet = Account.Create(Guid.CreateVersion7(), "Test wallet", LoadedAt.AddDays(-30));
        _accounts.Seed(_wallet);
        _groceries = SeedCategory("Test groceries");
        _cafe = SeedCategory("Test cafe");
    }

    // A statement lands every row on the one account the owner named, so without that account
    // there is nothing to load onto. Creating it would put a whole month on an account the owner
    // never asked for.
    [Fact]
    public async Task ExecuteAsync_NamedAccountNotInLedger_RefusesNamingTheAccountAndRecordsNothing()
    {
        SeedProfile(coverCafe: true);
        Guid unknownAccountId = Guid.CreateVersion7();

        Func<Task> load = () => LoadAsync(
            unknownAccountId,
            Row(3, StatementDay, 12.5m, GroceriesInFile));

        DomainRuleViolationException refusal =
            (await load.Should().ThrowAsync<DomainRuleViolationException>()).Which;
        refusal.Code.Should().Be("Account.NotFound");
        refusal.Message.Should().Be(UnknownAccountMessage);
        refusal.Details.Should().NotBeNull();
        refusal.Details!.Should().Contain("account_id", unknownAccountId);
        _accounts.Stored.Should().ContainSingle().Which.Should().BeSameAs(_wallet);
        AssertNothingWritten();
    }

    [Fact]
    public async Task ExecuteAsync_NoRulesForThisShape_RefusesSayingTheRulesAreMissingAndRecordsNothing()
    {
        Func<Task> load = () => LoadAsync(
            _wallet.Id,
            Row(3, StatementDay, 12.5m, GroceriesInFile));

        DomainRuleViolationException refusal =
            (await load.Should().ThrowAsync<DomainRuleViolationException>()).Which;
        refusal.Code.Should().Be("Import.MappingProfileNotConfirmed");
        refusal.Message.Should().Be(NoRulesMessage);
        refusal.Details.Should().NotBeNull();
        refusal.Details!.Should().Contain("shape_key", ShapeKey);
        AssertNothingWritten();
    }

    [Fact]
    public async Task ExecuteAsync_CoveredRows_RecordsThemOnTheNamedAccountWithTheCategoryTheRuleGives()
    {
        SeedProfile(coverCafe: true);

        ImportRun run = await LoadAsync(
            _wallet.Id,
            Row(3, StatementDay, 12.5m, GroceriesInFile, time: "23:40:12"),
            Row(4, StatementDay.AddDays(2), 4.8m, CafeInFile));

        run.Kind.Should().Be(ImportRunKind.Statement);
        run.AccountId.Should().Be(_wallet.Id);
        run.FileName.Should().Be(FileName);
        run.RecordedRowCount.Should().Be(2);
        run.SetAsideRows.Should().BeEmpty();
        _unitOfWork.Commits.Should().Be(1);
        _runs.Staged.Should().ContainSingle().Which.Should().BeSameAs(run);
        _transactions.Stored.Should().HaveCount(2).And.OnlyContain(transaction => transaction.AccountId == _wallet.Id);
        _transactions.Stored.Should().ContainSingle(transaction =>
            transaction.CategoryId == _groceries.Id
            && transaction.Amount == new Money(12.5m)
            && transaction.OccurredOn == StatementDay);
        _transactions.Stored.Should().ContainSingle(transaction =>
            transaction.CategoryId == _cafe.Id
            && transaction.Amount == new Money(4.8m)
            && transaction.OccurredOn == StatementDay.AddDays(2));
    }

    // The ledger is kept in the currency of the purchase. A card in another currency is billed
    // another figure for the same purchase, and that figure is not what the owner spent.
    [Fact]
    public async Task ExecuteAsync_PurchaseInLedgerCurrencyPaidFromCardInAnother_RecordsThePurchaseAmount()
    {
        SeedProfile(coverCafe: true);

        ImportRun run = await LoadAsync(
            _wallet.Id,
            Row(3, StatementDay, 34.5m, GroceriesInFile, cardAmount: -1500.25m, cardCurrency: "UAH"));

        run.RecordedRowCount.Should().Be(1);
        run.SetAsideRows.Should().BeEmpty();
        _transactions.Stored.Should().ContainSingle().Which.Amount.Should().Be(new Money(34.5m));
    }

    // A statement names only one side of a move between the owner's own accounts. Recording it as
    // spending would raise the month's spending by money that only moved.
    [Fact]
    public async Task ExecuteAsync_RuleSaysTransferBetweenOwnAccounts_SetsTheRowAsideWithThatReason()
    {
        SeedProfile(coverCafe: true);
        StatementRow moved = Row(3, StatementDay, 200m, OwnCardInFile);

        ImportRun run = await LoadAsync(_wallet.Id, moved);

        run.RecordedRowCount.Should().Be(0);
        SetAsideRow setAside = run.SetAsideRows.Should().ContainSingle().Subject;
        setAside.Reason.Should().Be(SetAsideReason.TransferBetweenOwnAccounts);
        setAside.SourceCategory.Should().Be(OwnCardInFile);
        setAside.RowNumber.Should().Be(moved.LineNumber);
        setAside.RawLine.Should().Be(moved.RawLine);
        _transactions.Stored.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_PurchaseInAnotherCurrency_SetsTheRowAsideAndStillRecordsTheOthers()
    {
        SeedProfile(coverCafe: true);
        StatementRow foreign = Row(
            3, StatementDay, 20m, GroceriesInFile, purchaseCurrency: "USD", cardAmount: -18.4m);

        ImportRun run = await LoadAsync(
            _wallet.Id,
            foreign,
            Row(4, StatementDay.AddDays(1), 4.8m, CafeInFile));

        run.RecordedRowCount.Should().Be(1);
        SetAsideRow setAside = run.SetAsideRows.Should().ContainSingle().Subject;
        setAside.Reason.Should().Be(SetAsideReason.ForeignCurrency);
        setAside.SourceCategory.Should().Be(GroceriesInFile);
        setAside.RowNumber.Should().Be(foreign.LineNumber);
        setAside.RawLine.Should().Be(foreign.RawLine);
        _transactions.Stored.Should().ContainSingle().Which.CategoryId.Should().Be(_cafe.Id);
    }

    // The card is billed in the ledger's currency here, but the purchase was not. The purchase
    // currency is the one that decides, because the purchase amount is the one recorded.
    [Fact]
    public async Task ExecuteAsync_PurchaseInAnotherCurrencyBilledInLedgerCurrency_SetsTheRowAside()
    {
        SeedProfile(coverCafe: true);

        ImportRun run = await LoadAsync(
            _wallet.Id,
            Row(3, StatementDay, 800m, GroceriesInFile, purchaseCurrency: "UAH", cardAmount: -18.4m));

        run.RecordedRowCount.Should().Be(0);
        run.SetAsideRows.Should().ContainSingle().Which.Reason.Should().Be(SetAsideReason.ForeignCurrency);
        _transactions.Stored.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_CategoryNoRuleCovers_SetsItsRowsAsideNamingTheCategoryAndRecordsTheCoveredRows()
    {
        SeedProfile(coverCafe: true);

        ImportRun run = await LoadAsync(
            _wallet.Id,
            Row(3, StatementDay, 12.5m, GroceriesInFile),
            Row(4, StatementDay.AddDays(1), 19.9m, BooksInFile),
            Row(5, StatementDay.AddDays(2), 7.25m, BooksInFile));

        run.RecordedRowCount.Should().Be(1);
        run.SetAsideRows.Should().HaveCount(2).And.OnlyContain(setAside =>
            setAside.Reason == SetAsideReason.NoRuleCovers && setAside.SourceCategory == BooksInFile);
        run.SetAsideRows.Select(setAside => setAside.RowNumber).Should().BeEquivalentTo([4, 5]);
        _transactions.Stored.Should().ContainSingle().Which.CategoryId.Should().Be(_groceries.Id);
    }

    [Fact]
    public async Task ExecuteAsync_CommitFails_RecordsNothingAndSaysTheLoadDidNotHappen()
    {
        SeedProfile(coverCafe: true);
        Transaction typedByHand = Transaction.Record(
            Guid.CreateVersion7(),
            _wallet.Id,
            _groceries.Id,
            new Money(30m),
            StatementDay.AddDays(-10),
            note: null,
            LoadedAt.AddDays(-20));
        _transactions.Seed(typedByHand);
        InvalidOperationException fault = new("Test fault at the commit.");
        FakeUnitOfWork failingUnitOfWork = new(() => throw fault);

        Func<Task> load = () => LoadAsync(
            failingUnitOfWork,
            _wallet.Id,
            Row(3, StatementDay, 12.5m, GroceriesInFile),
            Row(4, StatementDay.AddDays(1), 4.8m, CafeInFile));

        DomainRuleViolationException refusal =
            (await load.Should().ThrowAsync<DomainRuleViolationException>()).Which;
        refusal.Code.Should().Be("Import.NotRecorded");
        refusal.Message.Should().Be(NotRecordedMessage);
        refusal.InnerException.Should().BeSameAs(fault);
        _transactions.Stored.Should().ContainSingle().Which.Should().BeSameAs(typedByHand);
        _transactions.Committed(typedByHand.Id).Amount.Should().Be(30m);
        _runs.Stored.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_SameFileAgainAfterARuleIsAdded_RecordsOnlyTheRowsThatWereWaiting()
    {
        MappingProfile profile = SeedProfile(coverCafe: false);
        StatementRow[] file =
        [
            Row(3, StatementDay, 12.5m, GroceriesInFile),
            Row(4, StatementDay.AddDays(2), 4.8m, CafeInFile),
        ];

        ImportRun first = await LoadAsync(_wallet.Id, file);

        first.RecordedRowCount.Should().Be(1);
        first.SetAsideRows.Should().ContainSingle().Which.SourceCategory.Should().Be(CafeInFile);

        profile.AddRule(CategoryRule(CafeInFile, _cafe.Id));
        ImportRun second = await LoadAsync(_wallet.Id, file);

        second.RecordedRowCount.Should().Be(1);
        second.AlreadyPresentRowCount.Should().Be(1);
        second.SetAsideRows.Should().BeEmpty();
        _transactions.Stored.Should().HaveCount(2);
        _transactions.Stored.Should().ContainSingle(transaction => transaction.CategoryId == _groceries.Id);
        _transactions.Stored.Should().ContainSingle(transaction => transaction.CategoryId == _cafe.Id);
    }

    // A statement covers one account by definition. Rows from several cards share one file, and a
    // column that seems to name an account must not move a row onto another one.
    [Fact]
    public async Task ExecuteAsync_FileHasAColumnThatNamesAnotherAccount_RecordsEveryRowOnTheNamedAccount()
    {
        SeedProfile(coverCafe: true);
        Account other = Account.Create(Guid.CreateVersion7(), "Test card 0002", LoadedAt.AddDays(-30));
        _accounts.Seed(other);

        ImportRun run = await LoadAsync(
            new FakeStatementReader(Header(cardColumnReadAsAccount: true), [
                Row(3, StatementDay, 12.5m, GroceriesInFile, card: other.Name),
                Row(4, StatementDay.AddDays(1), 4.8m, CafeInFile, card: other.Id.ToString()),
            ]),
            _unitOfWork,
            _wallet.Id);

        run.RecordedRowCount.Should().Be(2);
        _transactions.Stored.Should().HaveCount(2).And.OnlyContain(transaction => transaction.AccountId == _wallet.Id);
    }

    // The reader throws InvalidDataException when the file turns out not to be a statement, for
    // example when it goes over the row ceiling. That is the owner handing over the wrong file, so
    // it is refused as such and never reported as a load that broke part-way.
    [Fact]
    public async Task ExecuteAsync_ReaderRefusesTheRows_IsRefusedAsAFileThatIsNotAStatement()
    {
        SeedProfile(coverCafe: true);
        InvalidDataException readFailure = new("The file has more than 1 rows.");
        UnreadableStatementReader reader = new(Header(), ReadStep.Rows, readFailure);

        Func<Task> load = () => LoadAsync(reader, _unitOfWork, _wallet.Id);

        DomainRuleViolationException refusal =
            (await load.Should().ThrowAsync<DomainRuleViolationException>()).Which;
        refusal.Code.Should().Be("Import.FileNotReadable");
        refusal.Message.Should().Be(NotReadableMessage);
        refusal.InnerException.Should().BeSameAs(readFailure);
        AssertNothingWritten();
    }

    [Fact]
    public async Task ExecuteAsync_ReaderRefusesTheHeader_IsRefusedAsAFileThatIsNotAStatement()
    {
        SeedProfile(coverCafe: true);
        InvalidDataException readFailure = new("The header row is not a statement header.");
        UnreadableStatementReader reader = new(Header(), ReadStep.Header, readFailure);

        Func<Task> load = () => LoadAsync(reader, _unitOfWork, _wallet.Id);

        DomainRuleViolationException refusal =
            (await load.Should().ThrowAsync<DomainRuleViolationException>()).Which;
        refusal.Code.Should().Be("Import.FileNotReadable");
        refusal.InnerException.Should().BeSameAs(readFailure);
        AssertNothingWritten();
    }

    [Theory]
    [InlineData(StatementColumn.Date, "date")]
    [InlineData(StatementColumn.Amount, "amount")]
    public async Task ExecuteAsync_StatementMissingDateOrAmount_RefusesNamingTheColumnAndRecordsNothing(
        StatementColumn missing,
        string missingName)
    {
        SeedProfile(coverCafe: true);
        FakeStatementReader reader = new(
            HeaderWithout(missing),
            [Row(3, StatementDay, 12.5m, GroceriesInFile)]);

        Func<Task> load = () => LoadAsync(reader, _unitOfWork, _wallet.Id);

        DomainRuleViolationException refusal =
            (await load.Should().ThrowAsync<DomainRuleViolationException>()).Which;
        refusal.Code.Should().Be("Import.MissingRequiredColumn");
        refusal.Message.Should().Be(MissingColumnMessage);
        refusal.Details.Should().NotBeNull().And.ContainKey("missing");
        refusal.Details!["missing"].Should().BeAssignableTo<IEnumerable<string>>()
            .Which.Should().Equal(missingName);
        AssertNothingWritten();
    }

    private static StatementRow Row(
        int lineNumber,
        DateOnly day,
        decimal purchaseAmount,
        string category,
        string purchaseCurrency = LedgerCurrency,
        decimal? cardAmount = null,
        string cardCurrency = LedgerCurrency,
        string card = FirstCard,
        string time = "12:30:00")
    {
        string[] values =
        [
            $"{day.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)} {time}",
            category,
            card,
            "Test Shop",
            (cardAmount ?? -purchaseAmount).ToString(CultureInfo.InvariantCulture),
            cardCurrency,
            purchaseAmount.ToString(CultureInfo.InvariantCulture),
            purchaseCurrency,
            "1000",
            cardCurrency,
        ];

        return new StatementRow(lineNumber, values, string.Join(',', values));
    }

    private static StatementHeader Header(bool cardColumnReadAsAccount = false)
    {
        Dictionary<StatementColumn, int> positions = new()
        {
            [StatementColumn.Date] = DateColumn,
            [StatementColumn.Category] = CategoryColumn,
            [StatementColumn.AmountSign] = CardAmountColumn,
            [StatementColumn.Amount] = PurchaseAmountColumn,
            [StatementColumn.Currency] = PurchaseCurrencyColumn,
        };
        HashSet<StatementColumn> missing = [];

        if (cardColumnReadAsAccount)
        {
            positions[StatementColumn.Account] = CardColumn;
        }
        else
        {
            missing.Add(StatementColumn.Account);
        }

        return new StatementHeader(ColumnNames, ShapeKey, missing, positions);
    }

    private static StatementHeader HeaderWithout(StatementColumn missing)
    {
        StatementHeader header = Header();
        Dictionary<StatementColumn, int> positions = new(header.ColumnPositions);
        positions.Remove(missing);

        return new StatementHeader(
            header.ColumnNames,
            header.ShapeKey,
            new HashSet<StatementColumn>(header.MissingColumns) { missing },
            positions);
    }

    private static MappingRule CategoryRule(string sourceCategory, Guid categoryId) =>
        MappingRule.Create(
            Guid.CreateVersion7(), sourceCategory, MappingRuleTarget.Category, categoryId, LoadedAt.AddDays(-5));

    private MappingProfile SeedProfile(bool coverCafe)
    {
        List<MappingRule> rules =
        [
            CategoryRule(GroceriesInFile, _groceries.Id),
            MappingRule.Create(
                Guid.CreateVersion7(),
                OwnCardInFile,
                MappingRuleTarget.TransferBetweenOwnAccounts,
                targetCategoryId: null,
                LoadedAt.AddDays(-5)),
        ];

        if (coverCafe)
        {
            rules.Add(CategoryRule(CafeInFile, _cafe.Id));
        }

        MappingProfile profile = MappingProfile.Create(Guid.CreateVersion7(), ShapeKey, rules, LoadedAt.AddDays(-5));
        _profiles.Seed(profile);

        return profile;
    }

    private Category SeedCategory(string name)
    {
        Category category = Category.Create(
            Guid.CreateVersion7(), name, CategoryKind.Expense, group: null, LoadedAt.AddDays(-30));
        _categories.Seed(category);

        return category;
    }

    private void CommitEverything()
    {
        _transactions.Commit();
        _transfers.Commit();
        _categories.Commit();
        _accounts.Commit();
    }

    private void AssertNothingWritten()
    {
        _unitOfWork.Commits.Should().Be(0);
        _runs.Staged.Should().BeEmpty();
        _transactions.Stored.Should().BeEmpty();
    }

    private Task<ImportRun> LoadAsync(Guid accountId, params StatementRow[] rows) =>
        LoadAsync(_unitOfWork, accountId, rows);

    private Task<ImportRun> LoadAsync(FakeUnitOfWork unitOfWork, Guid accountId, params StatementRow[] rows) =>
        LoadAsync(new FakeStatementReader(Header(), rows), unitOfWork, accountId);

    private async Task<ImportRun> LoadAsync(
        IStatementReader reader,
        FakeUnitOfWork unitOfWork,
        Guid accountId)
    {
        ImportPipeline pipeline = new(
            reader,
            _importQueries,
            _transactions,
            _runs,
            unitOfWork,
            new FixedClock(LoadedAt));

        LoadStatementUseCase loadStatement = new(
            reader,
            _accounts,
            _profiles,
            pipeline,
            new LedgerOptions(LedgerCurrency));

        return await loadStatement.ExecuteAsync(Stream.Null, FileName, accountId, CancellationToken.None);
    }

    private enum ReadStep
    {
        Header,
        Rows,
    }

    private sealed class UnreadableStatementReader(
        StatementHeader header,
        ReadStep failingStep,
        InvalidDataException readFailure) : IStatementReader
    {
        public Task<StatementHeader> ReadHeaderAsync(
            Stream statement,
            ImportFileKind kind,
            CancellationToken cancellationToken) =>
            failingStep == ReadStep.Header ? Task.FromException<StatementHeader>(readFailure) : Task.FromResult(header);

        public async IAsyncEnumerable<StatementRow> ReadRowsAsync(
            Stream statement,
            ImportFileKind kind,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return Row(3, StatementDay, 12.5m, GroceriesInFile);
            await Task.Yield();

            throw readFailure;
        }
    }
}
