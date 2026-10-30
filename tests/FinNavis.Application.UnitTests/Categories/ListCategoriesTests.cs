using FinNavis.Application.Categories;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;

namespace FinNavis.Application.UnitTests.Categories;

public sealed class ListCategoriesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 18, 0, 0, TimeSpan.Zero);

    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeCategoryGroupRepository _groups = new();

    // AC-33: both shapes of the read, each category with its name, its side and the group it sits
    // in right now — empty when it sits in none.
    [Fact]
    public async Task ListCategories_AllAndNarrowedToOneGroup_ReturnsNameSideAndGroup()
    {
        CategoryGroup utilities = SeedGroup("Utilities", CategoryKind.Expense);
        Category water = SeedCategory("Water", CategoryKind.Expense, utilities);
        Category gas = SeedCategory("Gas", CategoryKind.Expense, utilities);
        Category salary = SeedCategory("Salary", CategoryKind.Income, group: null);

        IReadOnlyList<Category> all = await ListAsync(categoryGroupId: null);
        IReadOnlyList<Category> narrowed = await ListAsync(utilities.Id);

        all.Select(category => (category.Id, category.Name, category.Kind, category.CategoryGroupId))
            .Should().BeEquivalentTo(
            [
                (water.Id, "Water", CategoryKind.Expense, (Guid?)utilities.Id),
                (gas.Id, "Gas", CategoryKind.Expense, (Guid?)utilities.Id),
                (salary.Id, "Salary", CategoryKind.Income, (Guid?)null),
            ]);

        narrowed.Select(category => category.Id).Should().BeEquivalentTo([water.Id, gas.Id]);
    }

    [Fact]
    public async Task ListCategories_GroupHoldingNothing_ReturnsAnEmptyList()
    {
        CategoryGroup travel = SeedGroup("Travel", CategoryKind.Expense);
        SeedCategory("Rent", CategoryKind.Expense, group: null);

        IReadOnlyList<Category> narrowed = await ListAsync(travel.Id);

        narrowed.Should().BeEmpty();
    }

    // AC-32. An empty list would say "this group holds nothing", which is a different fact from
    // "there is no such group".
    [Fact]
    public async Task ListCategories_UnknownGroup_IsRefused()
    {
        Guid unknownGroupId = Guid.CreateVersion7();

        Func<Task> list = () => ListAsync(unknownGroupId);

        var refusal = (await list.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("CategoryGroup.NotFound");
        refusal.Message.Should().Be($"No category group in the ledger has id {unknownGroupId}.");
    }

    [Fact]
    public async Task ListCategories_WhenCalled_PassesTheCancellationTokenThrough()
    {
        CategoryGroup utilities = SeedGroup("Utilities", CategoryKind.Expense);
        using var cancellation = new CancellationTokenSource();

        await new ListCategories(_categories, _groups).ExecuteAsync(utilities.Id, cancellation.Token);

        _categories.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
        _groups.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
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

    private Task<IReadOnlyList<Category>> ListAsync(Guid? categoryGroupId) =>
        new ListCategories(_categories, _groups).ExecuteAsync(categoryGroupId, CancellationToken.None);
}
