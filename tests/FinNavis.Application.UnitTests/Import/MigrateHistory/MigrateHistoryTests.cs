using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Application.Import.ConfirmMappingProfile;
using FinNavis.Application.Import.InspectFile;
using FinNavis.Application.Import.Pipeline;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;
using MigrateHistoryUseCase = FinNavis.Application.Import.MigrateHistory.MigrateHistory;

namespace FinNavis.Application.UnitTests.Import.MigrateHistory;

public sealed class MigrateHistoryTests
{
    private const string FileName = "test-export.csv";
    private const string WalletName = "Test cash wallet (UAH)";
    private const string CardName = "Test travel card (EUR)";
    private const string JarName = "Test savings jar (UAH)";
    private const string GroceriesName = "Test groceries";
    private const string SalaryName = "Test salary";
    private const string BooksName = "Test books";
    private const string BonusName = "Test bonus";
    private const string AccountBalanceCategory = "Account balance";

    private const string MissingColumnMessage =
        "This file does not say the date or the amount, or the account each row belongs to, "
        + "so nothing can be recorded from it.";

    private const string NotRecordedMessage =
        "The load stopped before it finished, so nothing from it was recorded and every balance is unchanged.";

    private const string NotReadableMessage = "This file could not be read as a statement, so nothing was recorded.";

    private const int DateColumn = 0;
    private const int AmountColumn = 1;
    private const int CurrencyColumn = 2;
    private const int AccountColumn = 3;
    private const int CategoryColumn = 4;

    private static readonly string[] ColumnNames =
        ["Date", "Amount", "Currency", "Account", "Category", "Category group", "Note"];

    private static readonly string ShapeKey =
        string.Join('|', ColumnNames.Select(name => name.ToLowerInvariant()));

    private static readonly DateTimeOffset LoadedAt = new(2026, 11, 7, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly January7 = new(2023, 1, 7);

    private readonly FakeAccountRepository _accounts = new();
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeImportRunRepository _runs = new();
    private readonly FakeTransactionRepository _transactions = new();
    private readonly FakeTransferRepository _transfers = new();
    private readonly FakeImportQueries _importQueries;
    private readonly FakeUnitOfWork _unitOfWork;
    private readonly Account _wallet;
    private readonly Account _card;
    private readonly Category _groceries;
    private readonly Category _salary;

    public MigrateHistoryTests()
    {
        _importQueries = new FakeImportQueries(_runs, _transactions, _transfers, _categories);
        _unitOfWork = new FakeUnitOfWork(CommitEverything);

        _wallet = SeedAccount(WalletName);
        _card = SeedAccount(CardName);
        _groceries = SeedCategory(GroceriesName, CategoryKind.Expense);
        _salary = SeedCategory(SalaryName, CategoryKind.Income);
    }

    // An export covers every account the owner kept, so a row that does not say its account has
    // nowhere to land. Guessing one would put years of history on the wrong account.
    [Theory]
    [InlineData(StatementColumn.Date, "date")]
    [InlineData(StatementColumn.Amount, "amount")]
    [InlineData(StatementColumn.Account, "account")]
    public async Task ExecuteAsync_ExportMissingARequiredColumn_RefusesNamingItAndRecordsNothing(
        StatementColumn missing,
        string missingName)
    {
        FakeStatementReader reader = new(
            Header(missing),
            [Row(2, January7, -12.5m, WalletName, BooksName)]);

        Func<Task> migrate = () => MigrateAsync(reader);

        DomainRuleViolationException refusal =
            (await migrate.Should().ThrowAsync<DomainRuleViolationException>()).Which;
        refusal.Code.Should().Be("Import.MissingRequiredColumn");
        refusal.Message.Should().Be(MissingColumnMessage);
        MissingNames(refusal).Should().BeEquivalentTo([missingName]);
        AssertNothingWritten();
    }

    [Fact]
    public async Task ExecuteAsync_ExportMissingAllThreeRequiredColumns_RefusesNamingEachOfThem()
    {
        FakeStatementReader reader = new(
            Header(StatementColumn.Date, StatementColumn.Amount, StatementColumn.Account),
            [Row(2, January7, -12.5m, WalletName, BooksName)]);

        Func<Task> migrate = () => MigrateAsync(reader);

        DomainRuleViolationException refusal =
            (await migrate.Should().ThrowAsync<DomainRuleViolationException>()).Which;
        refusal.Code.Should().Be("Import.MissingRequiredColumn");
        MissingNames(refusal).Should().BeEquivalentTo(["date", "amount", "account"]);
        AssertNothingWritten();
    }

    [Fact]
    public async Task ExecuteAsync_RowsNamingKnownAccountsAndCategories_RecordsEachOnWhatItNamesKeepingItsOwnDate()
    {
        FakeStatementReader reader = ExportReader(
            Row(2, January7, -12.5m, WalletName, GroceriesName),
            Row(3, January7.AddDays(-1), 1000m, CardName, SalaryName));

        ImportRun run = await MigrateAsync(reader);

        run.Kind.Should().Be(ImportRunKind.HistoryMigration);
        run.AccountId.Should().BeNull();
        run.FileName.Should().Be(FileName);
        run.RecordedRowCount.Should().Be(2);
        run.SetAsideRows.Should().BeEmpty();
        run.CreatedAccountLinks.Should().BeEmpty();
        run.CreatedCategoryLinks.Should().BeEmpty();
        _unitOfWork.Commits.Should().Be(1);
        _runs.Staged.Should().ContainSingle().Which.Should().BeSameAs(run);
        reader.KindsSeen.Should().NotBeEmpty().And.OnlyContain(kind => kind == ImportFileKind.Export);
        _transactions.Stored.Should().HaveCount(2);
        _transactions.Stored.Should().ContainSingle(transaction =>
            transaction.AccountId == _wallet.Id
            && transaction.CategoryId == _groceries.Id
            && transaction.Amount == new Money(12.5m)
            && transaction.OccurredOn == January7);
        _transactions.Stored.Should().ContainSingle(transaction =>
            transaction.AccountId == _card.Id
            && transaction.CategoryId == _salary.Id
            && transaction.Amount == new Money(1000m)
            && transaction.OccurredOn == January7.AddDays(-1));
        _accounts.Stored.Should().HaveCount(2);
        _categories.Stored.Should().HaveCount(2);
    }

    // The old tracker never held a move between the owner's own accounts, so every row of its
    // export is a transaction. A transfer built here would come from a guess, not from the file.
    [Fact]
    public async Task ExecuteAsync_AnyExport_RecordsNoTransfer()
    {
        FakeStatementReader reader = ExportReader(
            Row(2, January7, -200m, WalletName, GroceriesName),
            Row(3, January7, 200m, CardName, SalaryName));

        ImportRun run = await MigrateAsync(reader);

        run.TransferLinks.Should().BeEmpty();
        run.RecordedRowCount.Should().Be(2);
        _transfers.Stored.Should().BeEmpty();
        _transactions.Stored.Should().HaveCount(2);
    }

    // Undo must be able to remove what the load created, so each creation is linked to the run.
    // An account or category that already existed belongs to the owner and must never be linked.
    [Fact]
    public async Task ExecuteAsync_AccountAndCategoryTheLedgerLacks_CreatesEachOnceAndLinksItToTheRun()
    {
        FakeStatementReader reader = ExportReader(
            Row(2, January7, -20m, JarName, BooksName),
            Row(3, January7.AddDays(1), -5m, JarName, BooksName),
            Row(4, January7.AddDays(2), -7m, WalletName, GroceriesName));

        ImportRun run = await MigrateAsync(reader);

        Account jar = _accounts.Stored.Should().ContainSingle(account => account.Name == JarName).Subject;
        Category books = _categories.Stored.Should().ContainSingle(category => category.Name == BooksName).Subject;
        _accounts.Stored.Should().HaveCount(3);
        _categories.Stored.Should().HaveCount(3);
        books.Kind.Should().Be(CategoryKind.Expense);
        books.CategoryGroupId.Should().BeNull();
        run.CreatedAccountLinks.Should().ContainSingle().Which.AccountId.Should().Be(jar.Id);
        run.CreatedCategoryLinks.Should().ContainSingle().Which.CategoryId.Should().Be(books.Id);
        run.RecordedRowCount.Should().Be(3);
        _unitOfWork.Commits.Should().Be(1);
        _transactions.Stored.Where(transaction => transaction.AccountId == jar.Id)
            .Should().HaveCount(2).And.OnlyContain(transaction => transaction.CategoryId == books.Id);
        _transactions.Stored.Should().ContainSingle(transaction =>
            transaction.AccountId == _wallet.Id && transaction.CategoryId == _groceries.Id);
    }

    // The amount the ledger keeps is always positive and the category's side says which way the
    // money went, so the sign of the file's amount decides the side of a category it creates.
    [Fact]
    public async Task ExecuteAsync_IncomeRowNamingACategoryTheLedgerLacks_CreatesAnIncomeCategoryAndRecordsAPositiveAmount()
    {
        FakeStatementReader reader = ExportReader(
            Row(2, January7, 1500m, WalletName, BonusName));

        ImportRun run = await MigrateAsync(reader);

        Category bonus = _categories.Stored.Should().ContainSingle(category => category.Name == BonusName).Subject;
        bonus.Kind.Should().Be(CategoryKind.Income);
        run.CreatedCategoryLinks.Should().ContainSingle().Which.CategoryId.Should().Be(bonus.Id);
        Transaction recorded = _transactions.Stored.Should().ContainSingle().Subject;
        recorded.CategoryId.Should().Be(bonus.Id);
        recorded.Amount.Should().Be(new Money(1500m));
    }

    [Fact]
    public async Task ExecuteAsync_ExpenseRow_RecordsThePositiveAmountOnTheExpenseCategory()
    {
        FakeStatementReader reader = ExportReader(
            Row(2, January7, -42.75m, WalletName, GroceriesName));

        await MigrateAsync(reader);

        Transaction recorded = _transactions.Stored.Should().ContainSingle().Subject;
        recorded.CategoryId.Should().Be(_groceries.Id);
        recorded.Amount.Should().Be(new Money(42.75m));
    }

    // The owner kept typing a few months by hand before the move. Those entries are the owner's
    // own record; the load may only add to a month, never rewrite or drop what is already there.
    [Fact]
    public async Task ExecuteAsync_RowsInAMonthAlreadyRecordedByHand_AddsThemAndChangesNothingTypedByHand()
    {
        Transaction typedByHand = Transaction.Record(
            Guid.CreateVersion7(),
            _wallet.Id,
            _groceries.Id,
            new Money(30m),
            January7.AddDays(3),
            "Typed by hand",
            LoadedAt.AddDays(-20));
        _transactions.Seed(typedByHand);
        FakeTransactionRepository.Snapshot before = _transactions.Committed(typedByHand.Id);

        FakeStatementReader reader = ExportReader(
            Row(2, January7, -12.5m, WalletName, GroceriesName),
            Row(3, January7.AddDays(5), -4.8m, WalletName, GroceriesName));

        ImportRun run = await MigrateAsync(reader);

        run.RecordedRowCount.Should().Be(2);
        _transactions.StagedForRemoval.Should().BeEmpty();
        _transactions.Stored.Should().HaveCount(3)
            .And.OnlyContain(transaction => transaction.OccurredOn.Year == 2023 && transaction.OccurredOn.Month == 1);
        _transactions.Stored.Should().Contain(typedByHand);
        _transactions.Committed(typedByHand.Id).Should().Be(before);
        run.TransactionLinks.Should().NotContain(link => link.TransactionId == typedByHand.Id);
    }

    // The old tracker writes an opening balance per account as a row. It is not a movement, so it
    // is neither recorded nor set aside, and its category is not one the owner ever used.
    [Theory]
    [InlineData("0")]
    [InlineData("4221.58")]
    public async Task ExecuteAsync_AccountBalanceRow_IsDroppedWithoutRecordingOrSettingItAside(string amount)
    {
        FakeStatementReader reader = ExportReader(
            Row(2, January7, amount, WalletName, AccountBalanceCategory),
            Row(3, January7, "-12.5", WalletName, GroceriesName));

        ImportRun run = await MigrateAsync(reader);

        run.RecordedRowCount.Should().Be(1);
        run.AlreadyPresentRowCount.Should().Be(0);
        run.SetAsideRows.Should().BeEmpty();
        run.CreatedCategoryLinks.Should().BeEmpty();
        _categories.Stored.Should().NotContain(category => category.Name == AccountBalanceCategory);
        _transactions.Stored.Should().ContainSingle().Which.CategoryId.Should().Be(_groceries.Id);
    }

    [Fact]
    public async Task ExecuteAsync_CommitFails_RecordsNothingAndSaysTheLoadDidNotHappen()
    {
        InvalidOperationException fault = new("Test fault at the commit.");
        FakeUnitOfWork failingUnitOfWork = new(() => throw fault);
        FakeStatementReader reader = ExportReader(
            Row(2, January7, -20m, JarName, BooksName),
            Row(3, January7.AddDays(1), -12.5m, WalletName, GroceriesName));

        Func<Task> migrate = () => MigrateAsync(reader, failingUnitOfWork);

        DomainRuleViolationException refusal =
            (await migrate.Should().ThrowAsync<DomainRuleViolationException>()).Which;
        refusal.Code.Should().Be("Import.NotRecorded");
        refusal.Message.Should().Be(NotRecordedMessage);
        refusal.InnerException.Should().BeSameAs(fault);
        _runs.Stored.Should().BeEmpty();
        _transactions.Stored.Should().BeEmpty();
        _accounts.Stored.Should().HaveCount(2).And.NotContain(account => account.Name == JarName);
        _categories.Stored.Should().HaveCount(2).And.NotContain(category => category.Name == BooksName);
    }

    // The reader throws InvalidDataException when the file turns out not to be an export, for
    // example when it goes over the row ceiling. That is the owner handing over the wrong file, so
    // it is refused as such and never reported as a load that broke part-way.
    [Fact]
    public async Task ExecuteAsync_ReaderRefusesTheRows_IsRefusedAsAFileThatIsNotAStatement()
    {
        InvalidDataException readFailure = new("The file has more than 1 rows.");
        UnreadableExportReader reader = new(Header(), ReadStep.Rows, readFailure);

        Func<Task> migrate = () => MigrateAsync(reader);

        DomainRuleViolationException refusal =
            (await migrate.Should().ThrowAsync<DomainRuleViolationException>()).Which;
        refusal.Code.Should().Be("Import.FileNotReadable");
        refusal.Message.Should().Be(NotReadableMessage);
        refusal.InnerException.Should().BeSameAs(readFailure);
        AssertNothingWritten();
    }

    [Fact]
    public async Task ExecuteAsync_ReaderRefusesTheHeader_IsRefusedAsAFileThatIsNotAStatement()
    {
        InvalidDataException readFailure = new("The header row is not an export header.");
        UnreadableExportReader reader = new(Header(), ReadStep.Header, readFailure);

        Func<Task> migrate = () => MigrateAsync(reader);

        DomainRuleViolationException refusal =
            (await migrate.Should().ThrowAsync<DomainRuleViolationException>()).Which;
        refusal.Code.Should().Be("Import.FileNotReadable");
        refusal.InnerException.Should().BeSameAs(readFailure);
        AssertNothingWritten();
    }

    // The export already names accounts and categories in the owner's own words, so there is no
    // rule to look up and nothing for the owner to confirm before the history moves across.
    [Fact]
    public void MigrateHistory_DependsOnNoMappingProfileAndNoConfirmationStep()
    {
        Type[] forbidden =
        [
            typeof(IMappingProfileRepository),
            typeof(ConfirmMappingProfile),
            typeof(InspectFile),
        ];

        IEnumerable<Type> dependencies = typeof(MigrateHistoryUseCase)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType);

        dependencies.Should().NotIntersectWith(forbidden);
    }

    private static StatementRow Row(int lineNumber, DateOnly day, decimal amount, string account, string category) =>
        Row(lineNumber, day, amount.ToString(CultureInfo.InvariantCulture), account, category);

    private static StatementRow Row(int lineNumber, DateOnly day, string amount, string account, string category)
    {
        string[] values =
        [
            day.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture),
            amount,
            string.Empty,
            account,
            category,
            "Test group",
            "Test note",
        ];

        return new StatementRow(lineNumber, values, string.Join('\t', values));
    }

    private static StatementHeader Header(params StatementColumn[] missing)
    {
        Dictionary<StatementColumn, int> positions = new()
        {
            [StatementColumn.Date] = DateColumn,
            [StatementColumn.Amount] = AmountColumn,
            [StatementColumn.Currency] = CurrencyColumn,
            [StatementColumn.Account] = AccountColumn,
            [StatementColumn.Category] = CategoryColumn,
        };

        foreach (StatementColumn column in missing)
        {
            positions.Remove(column);
        }

        return new StatementHeader(ColumnNames, ShapeKey, new HashSet<StatementColumn>(missing), positions);
    }

    private static FakeStatementReader ExportReader(params StatementRow[] rows) => new(Header(), rows);

    private static IEnumerable<string> MissingNames(DomainRuleViolationException refusal)
    {
        refusal.Details.Should().NotBeNull().And.ContainKey("missing");

        return refusal.Details!["missing"].Should().BeAssignableTo<IEnumerable<string>>().Subject;
    }

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
        _accounts.Stored.Should().HaveCount(2);
        _categories.Stored.Should().HaveCount(2);
    }

    private async Task<ImportRun> MigrateAsync(IStatementReader reader, FakeUnitOfWork? unitOfWork = null)
    {
        ImportPipeline pipeline = new(
            reader,
            _importQueries,
            _transactions,
            _runs,
            unitOfWork ?? _unitOfWork,
            new FixedClock(LoadedAt));

        MigrateHistoryUseCase migrateHistory = new(
            reader,
            _accounts,
            _categories,
            pipeline,
            new FixedClock(LoadedAt));

        return await migrateHistory.ExecuteAsync(Stream.Null, FileName, CancellationToken.None);
    }

    private enum ReadStep
    {
        Header,
        Rows,
    }

    private sealed class UnreadableExportReader(
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
            yield return Row(2, January7, -20m, JarName, BooksName);
            await Task.Yield();

            throw readFailure;
        }
    }
}
