using System.Globalization;
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
public sealed class BreakdownQueryTests : IAsyncLifetime
{
    private static readonly DateRange March = DateRange.Create(Day("2026-03-01"), Day("2026-03-31"));

    private readonly PostgresFixture _database;

    public BreakdownQueryTests(PostgresFixture database) => _database = database;

    public Task InitializeAsync() => _database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // Spending and earning are totalled apart and never added together.
    [Fact]
    public async Task Breakdown_PeriodWithSpendingAndEarning_KeepsSidesApart()
    {
        Account wallet = LedgerData.NewAccount("Cash wallet");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);
        Category salary = LedgerData.NewCategory("Salary", CategoryKind.Income);

        await StoreAsync(
            [wallet],
            [],
            [groceries, salary],
            [
                Movement(wallet.Id, groceries.Id, 30m, "2026-03-05"),
                Movement(wallet.Id, groceries.Id, 12.50m, "2026-03-09"),
                Movement(wallet.Id, salary.Id, 500m, "2026-03-01"),
            ],
            []);

        IReadOnlyList<BreakdownCategoryRow> rows = await ReadAsync(March, accountId: null);

        rows.Should().HaveCount(2);
        Row(rows, groceries.Id).Kind.Should().Be(CategoryKind.Expense);
        Row(rows, groceries.Id).Total.Should().Be(new Money(42.50m));
        Row(rows, salary.Id).Kind.Should().Be(CategoryKind.Income);
        Row(rows, salary.Id).Total.Should().Be(new Money(500m));
    }

    // A transfer is not spending and not earning. The statement cannot reach the transfers
    // table at all, so this holds structurally rather than by a filter.
    [Fact]
    public async Task Breakdown_PeriodWithTransfers_CountsNoTransfer()
    {
        Account wallet = LedgerData.NewAccount("Cash wallet");
        Account jar = LedgerData.NewAccount("Savings jar");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);

        await StoreAsync(
            [wallet, jar],
            [],
            [groceries],
            [Movement(wallet.Id, groceries.Id, 30m, "2026-03-05")],
            [Move(wallet.Id, jar.Id, 900m, "2026-03-06")]);

        IReadOnlyList<BreakdownCategoryRow> rows = await ReadAsync(March, accountId: null);

        rows.Should().ContainSingle().Which.Total.Should().Be(new Money(30m));
        LedgerBreakdownQueries.Sql.Should().NotContain("transfers");
    }

    // The query half: every category of the period comes back exactly once, so the group totals
    // the use case builds plus the ungrouped total add up to the period total per side.
    [Fact]
    public async Task Breakdown_PeriodWithGroupedAndUngroupedCategories_TotalsAddUpPerSide()
    {
        Account wallet = LedgerData.NewAccount("Cash wallet");
        CategoryGroup utilities = LedgerData.NewGroup("Utilities", CategoryKind.Expense);
        Category electricity = LedgerData.NewCategory("Electricity", CategoryKind.Expense, utilities);
        Category water = LedgerData.NewCategory("Water", CategoryKind.Expense, utilities);
        Category coffee = LedgerData.NewCategory("Coffee", CategoryKind.Expense);

        await StoreAsync(
            [wallet],
            [utilities],
            [electricity, water, coffee],
            [
                Movement(wallet.Id, electricity.Id, 60m, "2026-03-05"),
                Movement(wallet.Id, water.Id, 15m, "2026-03-06"),
                Movement(wallet.Id, coffee.Id, 25m, "2026-03-07"),
            ],
            []);

        IReadOnlyList<BreakdownCategoryRow> rows = await ReadAsync(March, accountId: null);

        Money grouped = Total(rows.Where(row => row.CategoryGroupId == utilities.Id));
        Money ungrouped = Total(rows.Where(row => row.CategoryGroupId is null));

        grouped.Should().Be(new Money(75m));
        ungrouped.Should().Be(new Money(25m));
        (grouped + ungrouped).Should().Be(Total(rows));
        Total(rows).Should().Be(new Money(100m));
    }

    // A category with no group is not in a group called Default. The row carries no group at all,
    // and the Default bucket is produced later, by the use case.
    [Fact]
    public async Task Breakdown_CategoryWithNoGroup_CarriesNoGroupRatherThanADefaultOne()
    {
        Account wallet = LedgerData.NewAccount("Cash wallet");
        Category coffee = LedgerData.NewCategory("Coffee", CategoryKind.Expense);

        await StoreAsync([wallet], [], [coffee], [Movement(wallet.Id, coffee.Id, 25m, "2026-03-07")], []);

        BreakdownCategoryRow row = (await ReadAsync(March, accountId: null)).Should().ContainSingle().Subject;

        row.CategoryGroupId.Should().BeNull();
        row.CategoryGroupName.Should().BeNull();
    }

    // The mapping is read at query time, so a move changes the group figures at once and
    // leaves the category's own figure for the closed period alone.
    [Fact]
    public async Task Breakdown_AfterACategoryMovesToAnotherGroup_FollowsTheCurrentMapping()
    {
        Account wallet = LedgerData.NewAccount("Cash wallet");
        CategoryGroup utilities = LedgerData.NewGroup("Utilities", CategoryKind.Expense);
        CategoryGroup home = LedgerData.NewGroup("Home", CategoryKind.Expense);
        Category electricity = LedgerData.NewCategory("Electricity", CategoryKind.Expense, utilities);

        await StoreAsync(
            [wallet],
            [utilities, home],
            [electricity],
            [Movement(wallet.Id, electricity.Id, 60m, "2026-03-05")],
            []);

        await using (var write = _database.CreateDbContext())
        {
            var categories = new CategoryRepository(write);
            var groups = new CategoryGroupRepository(write);

            Category loaded = (await categories.GetByIdAsync(electricity.Id, CancellationToken.None))!;
            loaded.PlaceInGroup((await groups.GetByIdAsync(home.Id, CancellationToken.None))!);

            await new UnitOfWork(write).SaveChangesAsync(CancellationToken.None);
        }

        BreakdownCategoryRow row = (await ReadAsync(March, accountId: null)).Should().ContainSingle().Subject;

        row.CategoryGroupId.Should().Be(home.Id);
        row.CategoryGroupName.Should().Be("Home");
        row.Total.Should().Be(new Money(60m));
    }

    [Fact]
    public async Task Breakdown_FilteredByAccount_TotalsOnlyThatAccountsTransactions()
    {
        Account wallet = LedgerData.NewAccount("Cash wallet");
        Account card = LedgerData.NewAccount("Debit card");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);

        await StoreAsync(
            [wallet, card],
            [],
            [groceries],
            [
                Movement(wallet.Id, groceries.Id, 30m, "2026-03-05"),
                Movement(card.Id, groceries.Id, 70m, "2026-03-05"),
            ],
            []);

        IReadOnlyList<BreakdownCategoryRow> rows = await ReadAsync(March, wallet.Id);

        rows.Should().ContainSingle().Which.Total.Should().Be(new Money(30m));
    }

    [Fact]
    public async Task Breakdown_WithMovementsOutsideThePeriod_LeavesThemOut()
    {
        Account wallet = LedgerData.NewAccount("Cash wallet");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);

        await StoreAsync(
            [wallet],
            [],
            [groceries],
            [
                Movement(wallet.Id, groceries.Id, 10m, "2026-02-28"),
                Movement(wallet.Id, groceries.Id, 20m, "2026-03-01"),
                Movement(wallet.Id, groceries.Id, 40m, "2026-03-31"),
                Movement(wallet.Id, groceries.Id, 80m, "2026-04-01"),
            ],
            []);

        IReadOnlyList<BreakdownCategoryRow> rows = await ReadAsync(March, accountId: null);

        // Both ends of the period count as inside.
        rows.Should().ContainSingle().Which.Total.Should().Be(new Money(60m));
    }

    [Fact]
    public async Task Breakdown_PeriodThatRecordedNothing_ReturnsNoRows()
    {
        Account wallet = LedgerData.NewAccount("Cash wallet");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);

        await StoreAsync([wallet], [], [groceries], [Movement(wallet.Id, groceries.Id, 10m, "2026-02-28")], []);

        IReadOnlyList<BreakdownCategoryRow> rows = await ReadAsync(March, accountId: null);

        rows.Should().BeEmpty();
    }

    private static BreakdownCategoryRow Row(IEnumerable<BreakdownCategoryRow> rows, Guid categoryId) =>
        rows.Single(row => row.CategoryId == categoryId);

    private static Money Total(IEnumerable<BreakdownCategoryRow> rows) =>
        rows.Aggregate(Money.Zero, (total, row) => total + row.Total);

    private static DateOnly Day(string day) =>
        DateOnly.ParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static Transaction Movement(Guid accountId, Guid categoryId, decimal amount, string day) =>
        Transaction.Record(
            LedgerData.NewId(),
            accountId,
            categoryId,
            new Money(amount),
            Day(day),
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

    private async Task<IReadOnlyList<BreakdownCategoryRow>> ReadAsync(DateRange period, Guid? accountId)
    {
        await using var context = _database.CreateDbContext();

        return await new LedgerBreakdownQueries(context).GetBreakdownAsync(period, accountId, CancellationToken.None);
    }

    private async Task StoreAsync(
        IReadOnlyList<Account> accounts,
        IReadOnlyList<CategoryGroup> groups,
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

        var groupRepository = new CategoryGroupRepository(context);
        foreach (CategoryGroup group in groups)
        {
            groupRepository.Add(group);
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
}
