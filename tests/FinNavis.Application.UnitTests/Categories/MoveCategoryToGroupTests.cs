using FinNavis.Application.Categories;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.UnitTests.Categories;

public sealed class MoveCategoryToGroupTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly RecordedOn = new(2026, 9, 14);

    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeCategoryGroupRepository _groups = new();
    private readonly FakeUnitOfWork _unitOfWork;

    public MoveCategoryToGroupTests() => _unitOfWork = new FakeUnitOfWork(Commit);

    // Only the category's group link changes, so every entry already filed under it keeps
    // its amount and its category id — the figure is the same sum before and after. What moves is
    // where the breakdown rolls that figure up from the next read on.
    [Fact]
    public async Task MoveCategoryToGroup_CategoryWithHistory_FigureUnchanged()
    {
        CategoryGroup utilities = SeedGroup("Utilities", CategoryKind.Expense);
        CategoryGroup household = SeedGroup("Household", CategoryKind.Expense);
        Category water = SeedCategory("Water", CategoryKind.Expense, utilities);
        Transaction[] recorded = [RecordedUnder(water, 42.5000m), RecordedUnder(water, 17.2500m)];
        Money figureBefore = Total(recorded);

        Category moved = await MoveAsync(water.Id, household.Id);

        moved.CategoryGroupId.Should().Be(household.Id);
        recorded.Should().AllSatisfy(entry => entry.CategoryId.Should().Be(water.Id));
        Total(recorded).Should().Be(figureBefore);
        _unitOfWork.Commits.Should().Be(1);
    }

    // The other half: out of every group. Nothing is assigned — the link is cleared, and the
    // category does not join a group called Default, because there is no such row.
    [Fact]
    public async Task MoveCategoryToGroup_WithNoGroupNamed_TakesItOutOfEveryGroup()
    {
        CategoryGroup utilities = SeedGroup("Utilities", CategoryKind.Expense);
        Category water = SeedCategory("Water", CategoryKind.Expense, utilities);

        Category moved = await MoveAsync(water.Id, categoryGroupId: null);

        moved.CategoryGroupId.Should().BeNull();
        _categories.CommittedGroupLinks[water.Id].Should().BeNull();
        _groups.Stored.Should().ContainSingle().Which.Id.Should().Be(utilities.Id);
    }

    [Fact]
    public async Task MoveCategoryToGroup_GroupOnOtherSide_IsRefused()
    {
        CategoryGroup earning = SeedGroup("Salary", CategoryKind.Income);
        Category water = SeedCategory("Water", CategoryKind.Expense, group: null);

        Func<Task> move = () => MoveAsync(water.Id, earning.Id);

        var refusal = (await move.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Category.GroupKindMismatch");
        refusal.Message.Should().Be("An expense category cannot sit in an income category group.");
        _categories.CommittedGroupLinks[water.Id].Should().BeNull();
        _unitOfWork.Commits.Should().Be(0);
    }

    // On the move side the group is named back and it is not created on the way.
    [Fact]
    public async Task MoveCategoryToGroup_UnknownGroup_IsRefusedAndCreatesNothing()
    {
        Category water = SeedCategory("Water", CategoryKind.Expense, group: null);
        Guid unknownGroupId = Guid.CreateVersion7();

        Func<Task> move = () => MoveAsync(water.Id, unknownGroupId);

        var refusal = (await move.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("CategoryGroup.NotFound");
        refusal.Message.Should().Be($"No category group in the ledger has id {unknownGroupId}.");
        _groups.Stored.Should().BeEmpty();
        _unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task MoveCategoryToGroup_UnknownCategoryId_IsRefusedAsNotFound()
    {
        CategoryGroup utilities = SeedGroup("Utilities", CategoryKind.Expense);
        Guid unknownId = Guid.CreateVersion7();

        Func<Task> move = () => MoveAsync(unknownId, utilities.Id);

        var refusal = (await move.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Category.NotFound");
        refusal.Message.Should().Be($"No category in the ledger has id {unknownId}.");
        _unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task MoveCategoryToGroup_WhenCalled_PassesTheCancellationTokenThrough()
    {
        CategoryGroup utilities = SeedGroup("Utilities", CategoryKind.Expense);
        Category water = SeedCategory("Water", CategoryKind.Expense, group: null);
        using var cancellation = new CancellationTokenSource();

        await new MoveCategoryToGroup(_categories, _groups, _unitOfWork)
            .ExecuteAsync(water.Id, utilities.Id, cancellation.Token);

        _categories.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
        _groups.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
        _unitOfWork.TokensSeen.Should().AllSatisfy(token => token.Should().Be(cancellation.Token));
    }

    private static Transaction RecordedUnder(Category category, decimal amount) =>
        Transaction.Record(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            category.Id,
            new Money(amount),
            RecordedOn,
            note: null,
            Now);

    private static Money Total(IEnumerable<Transaction> entries) =>
        entries.Aggregate(Money.Zero, (sum, entry) => sum + entry.Amount);

    private void Commit()
    {
        _categories.Commit();
        _groups.Commit();
    }

    private CategoryGroup SeedGroup(string name, CategoryKind kind)
    {
        CategoryGroup group = CategoryGroup.Create(Guid.CreateVersion7(), name, kind, Now);
        _groups.Seed(group);

        return group;
    }

    private Category SeedCategory(string name, CategoryKind kind, CategoryGroup? group)
    {
        Category category = Category.Create(Guid.CreateVersion7(), name, kind, group, Now);
        _categories.Seed(category);

        return category;
    }

    private Task<Category> MoveAsync(Guid categoryId, Guid? categoryGroupId) =>
        new MoveCategoryToGroup(_categories, _groups, _unitOfWork)
            .ExecuteAsync(categoryId, categoryGroupId, CancellationToken.None);
}
