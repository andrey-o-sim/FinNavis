using FinNavis.Application.Categories;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.UnitTests.Categories;

public sealed class RenameCategoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly ClosedPeriodDay = new(2026, 9, 14);

    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeUnitOfWork _unitOfWork;

    public RenameCategoryTests() => _unitOfWork = new FakeUnitOfWork(_categories.Commit);

    // The figure for a closed period is a sum over the transactions filed under the
    // category id. A rename touches the category row only, so every entry keeps its id, its
    // category and its amount, and the sum cannot move.
    [Fact]
    public async Task RenameCategory_CategoryWithHistory_KeepsClosedPeriodFigure()
    {
        Category groceries = SeedCategory("Groceries", CategoryKind.Expense);
        Transaction[] closedPeriod =
        [
            RecordedUnder(groceries, 42.5000m),
            RecordedUnder(groceries, 17.2500m),
        ];
        Money figureBefore = Total(closedPeriod);

        Category renamed = await RenameAsync(groceries.Id, "Food and drink");

        renamed.Id.Should().Be(groceries.Id);
        renamed.Name.Should().Be("Food and drink");
        renamed.Kind.Should().Be(CategoryKind.Expense);

        closedPeriod.Select(entry => (entry.CategoryId, entry.Amount, entry.OccurredOn))
            .Should().AllSatisfy(entry => entry.CategoryId.Should().Be(groceries.Id));
        Total(closedPeriod).Should().Be(figureBefore);
    }

    [Fact]
    public async Task RenameCategory_NameTakenOnSameSide_IsRefused()
    {
        Category groceries = SeedCategory("Groceries", CategoryKind.Expense);
        SeedCategory("Rent", CategoryKind.Expense);

        Func<Task> rename = () => RenameAsync(groceries.Id, "Rent");

        var refusal = (await rename.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Category.DuplicateName");
        refusal.Message.Should().Be("An expense category named 'Rent' already exists.");
        _unitOfWork.Commits.Should().Be(0);
    }

    // The other half: the same name on the other side belongs to another category.
    [Fact]
    public async Task RenameCategory_NameTakenOnTheOtherSide_IsAccepted()
    {
        Category spending = SeedCategory("Groceries", CategoryKind.Expense);
        SeedCategory("Gifts", CategoryKind.Income);

        Category renamed = await RenameAsync(spending.Id, "Gifts");

        renamed.Name.Should().Be("Gifts");
        renamed.Kind.Should().Be(CategoryKind.Expense);
    }

    // ExistsWithNameAsync answers true for the category's own row, so keeping the current name
    // must not read as a clash.
    [Fact]
    public async Task RenameCategory_ToItsOwnCurrentName_IsAccepted()
    {
        Category groceries = SeedCategory("Groceries", CategoryKind.Expense);

        Category renamed = await RenameAsync(groceries.Id, "Groceries");

        renamed.Name.Should().Be("Groceries");
        _unitOfWork.Commits.Should().Be(1);
    }

    [Fact]
    public async Task RenameCategory_UnknownCategoryId_IsRefusedAsNotFound()
    {
        Guid unknownId = Guid.CreateVersion7();

        Func<Task> rename = () => RenameAsync(unknownId, "Groceries");

        var refusal = (await rename.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Category.NotFound");
        refusal.Message.Should().Be($"No category in the ledger has id {unknownId}.");
        _unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task RenameCategory_WithABlankName_IsRefusedAndLeavesTheNameAsItWas()
    {
        Category groceries = SeedCategory("Groceries", CategoryKind.Expense);

        Func<Task> rename = () => RenameAsync(groceries.Id, "   ");

        (await rename.Should().ThrowAsync<DomainRuleViolationException>())
            .Which.Code.Should().Be("Category.NameInvalid");

        _categories.Stored.Should().ContainSingle().Which.Name.Should().Be("Groceries");
        _unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task RenameCategory_WhenCalled_PassesTheCancellationTokenThrough()
    {
        Category groceries = SeedCategory("Groceries", CategoryKind.Expense);
        using var cancellation = new CancellationTokenSource();

        await new RenameCategory(_categories, _unitOfWork)
            .ExecuteAsync(groceries.Id, "Food and drink", cancellation.Token);

        _categories.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
        _unitOfWork.TokensSeen.Should().AllSatisfy(token => token.Should().Be(cancellation.Token));
    }

    private static Transaction RecordedUnder(Category category, decimal amount) =>
        Transaction.Record(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            category.Id,
            new Money(amount),
            ClosedPeriodDay,
            note: null,
            Now);

    private static Money Total(IEnumerable<Transaction> entries) =>
        entries.Aggregate(Money.Zero, (sum, entry) => sum + entry.Amount);

    private Category SeedCategory(string name, CategoryKind kind)
    {
        Category category = Category.Create(Guid.CreateVersion7(), name, kind, group: null, Now);
        _categories.Seed(category);

        return category;
    }

    private Task<Category> RenameAsync(Guid categoryId, string name) =>
        new RenameCategory(_categories, _unitOfWork).ExecuteAsync(categoryId, name, CancellationToken.None);
}
