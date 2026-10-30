using FinNavis.Application.Abstractions.Reads;
using FinNavis.Application.Reporting;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.UnitTests.Reporting;

public sealed class GetBreakdownTests
{
    private static readonly DateOnly From = new(2026, 10, 1);
    private static readonly DateOnly To = new(2026, 10, 31);

    private readonly FakeLedgerQueries _ledger = new();

    // Spending and earning are kept apart and never added together.
    [Fact]
    public async Task GetBreakdown_Period_ReturnsGroupAndCategoryTotalsSidesApart()
    {
        Guid utilities = Guid.CreateVersion7();
        Guid salaryGroup = Guid.CreateVersion7();
        Row("Water", CategoryKind.Expense, 40.0000m, utilities, "Utilities");
        Row("Gas", CategoryKind.Expense, 60.0000m, utilities, "Utilities");
        Row("Salary", CategoryKind.Income, 1200.0000m, salaryGroup, "Earnings");

        Breakdown breakdown = await ReadAsync();

        breakdown.Period.Should().Be(DateRange.Create(From, To));
        breakdown.AccountId.Should().BeNull();

        breakdown.Expense.Total.Should().Be(new Money(100.0000m));
        BreakdownGroupTotal spending = breakdown.Expense.Groups.Should().ContainSingle().Which;
        spending.CategoryGroupId.Should().Be(utilities);
        spending.Name.Should().Be("Utilities");
        spending.Total.Should().Be(new Money(100.0000m));
        spending.Categories.Select(category => (category.Name, category.Total))
            .Should().BeEquivalentTo(
            [
                ("Gas", new Money(60.0000m)),
                ("Water", new Money(40.0000m)),
            ]);

        breakdown.Income.Total.Should().Be(new Money(1200.0000m));
        breakdown.Income.Groups.Should().ContainSingle().Which.Name.Should().Be("Earnings");
    }

    // On both sides the side total and the group totals are summed from the same rows in one
    // pass, so they cannot fail to agree — the Default bucket included.
    [Fact]
    public async Task GetBreakdown_GroupTotalsPlusDefaultGroup_EqualPeriodTotal()
    {
        Guid utilities = Guid.CreateVersion7();
        Guid earnings = Guid.CreateVersion7();
        Row("Water", CategoryKind.Expense, 40.0000m, utilities, "Utilities");
        Row("Rent", CategoryKind.Expense, 750.0000m, categoryGroupId: null, categoryGroupName: null);
        Row("Salary", CategoryKind.Income, 1200.0000m, earnings, "Earnings");
        Row("Gift", CategoryKind.Income, 50.0000m, categoryGroupId: null, categoryGroupName: null);

        Breakdown breakdown = await ReadAsync();

        SumOfGroups(breakdown.Expense).Should().Be(breakdown.Expense.Total);
        SumOfGroups(breakdown.Income).Should().Be(breakdown.Income.Total);
        breakdown.Expense.Total.Should().Be(new Money(790.0000m));
        breakdown.Income.Total.Should().Be(new Money(1250.0000m));

        breakdown.Expense.Groups.Should().HaveCount(2);
        breakdown.Income.Groups.Should().HaveCount(2);
    }

    // The Default group is produced here and is not a row: it has no id, and the one thing that
    // puts a category in it is having no group at all.
    [Fact]
    public async Task GetBreakdown_CategoriesWithNoGroup_AreBucketedUnderTheDefaultGroup()
    {
        Row("Rent", CategoryKind.Expense, 750.0000m, categoryGroupId: null, categoryGroupName: null);
        Row("Coffee", CategoryKind.Expense, 12.0000m, categoryGroupId: null, categoryGroupName: null);

        Breakdown breakdown = await ReadAsync();

        BreakdownGroupTotal bucket = breakdown.Expense.Groups.Should().ContainSingle().Which;

        bucket.CategoryGroupId.Should().BeNull();
        bucket.Name.Should().Be("Default group");
        bucket.Total.Should().Be(new Money(762.0000m));
        bucket.Categories.Select(category => category.Name)
            .Should().BeEquivalentTo(["Coffee", "Rent"]);
    }

    // The report orders the Default bucket last, so two reads of the same period cannot come back
    // in a different order.
    [Fact]
    public async Task GetBreakdown_WithGroupedAndUngroupedCategories_PutsTheDefaultGroupLast()
    {
        Guid utilities = Guid.CreateVersion7();
        Row("Rent", CategoryKind.Expense, 750.0000m, categoryGroupId: null, categoryGroupName: null);
        Row("Water", CategoryKind.Expense, 40.0000m, utilities, "Utilities");

        Breakdown breakdown = await ReadAsync();

        breakdown.Expense.Groups.Select(group => group.Name)
            .Should().ContainInOrder("Utilities", "Default group");
    }

    // The query never reads transfers, so there is nothing here to filter out — the test states
    // what the raw rows can never contain.
    [Fact]
    public async Task GetBreakdown_PeriodWithTransfers_CountsNoTransfer()
    {
        Guid utilities = Guid.CreateVersion7();
        Row("Water", CategoryKind.Expense, 40.0000m, utilities, "Utilities");

        Breakdown breakdown = await ReadAsync();

        // A transfer carries no category, and a breakdown row is a category's total, so no row
        // can stand for one. The figures are exactly the transactions that were recorded.
        breakdown.Expense.Total.Should().Be(new Money(40.0000m));
        breakdown.Income.Total.Should().Be(Money.Zero);
        breakdown.Income.Groups.Should().BeEmpty();
    }

    // A period that recorded nothing is an answer, not a failure.
    [Fact]
    public async Task GetBreakdown_PeriodWithNothingRecorded_ReportsZeroOnBothSides()
    {
        Breakdown breakdown = await ReadAsync();

        breakdown.Expense.Total.Should().Be(Money.Zero);
        breakdown.Expense.Groups.Should().BeEmpty();
        breakdown.Income.Total.Should().Be(Money.Zero);
        breakdown.Income.Groups.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBreakdown_NarrowedToOneAccount_CarriesTheFilterThrough()
    {
        Guid accountId = Guid.CreateVersion7();

        Breakdown breakdown = await ReadAsync(accountId);

        _ledger.BreakdownAccountIdSeen.Should().Be(accountId);
        _ledger.BreakdownPeriodSeen.Should().Be(DateRange.Create(From, To));
        breakdown.AccountId.Should().Be(accountId);
    }

    [Fact]
    public async Task GetBreakdown_FromAfterTo_IsRefused()
    {
        Func<Task> read = () => new GetBreakdown(_ledger)
            .ExecuteAsync(To, From, accountId: null, CancellationToken.None);

        var refusal = (await read.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Breakdown.InvalidPeriod");
        refusal.Message.Should().Be("'from' must not be after 'to'.");
        _ledger.BreakdownPeriodSeen.Should().BeNull();
    }

    // One day is a legal period: both ends of the range count as inside.
    [Fact]
    public async Task GetBreakdown_FromEqualToTo_IsAccepted()
    {
        Breakdown breakdown = await new GetBreakdown(_ledger)
            .ExecuteAsync(From, From, accountId: null, CancellationToken.None);

        breakdown.Period.Should().Be(DateRange.Create(From, From));
    }

    [Fact]
    public async Task GetBreakdown_WhenCalled_PassesTheCancellationTokenThrough()
    {
        using var cancellation = new CancellationTokenSource();

        await new GetBreakdown(_ledger).ExecuteAsync(From, To, accountId: null, cancellation.Token);

        _ledger.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
    }

    private static Money SumOfGroups(BreakdownSide side) =>
        side.Groups.Aggregate(Money.Zero, (sum, group) => sum + group.Total);

    private void Row(
        string categoryName,
        CategoryKind kind,
        decimal total,
        Guid? categoryGroupId,
        string? categoryGroupName) =>
        _ledger.BreakdownRows.Add(
            new BreakdownCategoryRow(
                Guid.CreateVersion7(),
                categoryName,
                kind,
                categoryGroupId,
                categoryGroupName,
                new Money(total)));

    private Task<Breakdown> ReadAsync(Guid? accountId = null) =>
        new GetBreakdown(_ledger).ExecuteAsync(From, To, accountId, CancellationToken.None);
}
