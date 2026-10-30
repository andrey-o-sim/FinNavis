using FinNavis.Application.CategoryGroups;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;

namespace FinNavis.Application.UnitTests.CategoryGroups;

public sealed class ListCategoryGroupsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 18, 0, 0, TimeSpan.Zero);

    private readonly FakeCategoryGroupRepository _groups = new();
    private readonly FakeCategoryRepository _categories = new();

    // AC-27. The list reads category_groups alone and never joins categories, so a group holding
    // nothing cannot fall out of it.
    [Fact]
    public async Task ListCategoryGroups_ReturnsNameAndSide_IncludingEmptyGroups()
    {
        CategoryGroup utilities = Seed("Utilities", CategoryKind.Expense);
        CategoryGroup salary = Seed("Salary", CategoryKind.Income);
        _categories.Seed(Category.Create(Guid.CreateVersion7(), "Water", CategoryKind.Expense, utilities, Now));

        IReadOnlyList<CategoryGroup> listed = await ListAsync();

        listed.Should().HaveCount(2);
        listed.Select(group => (group.Id, group.Name, group.Kind))
            .Should().BeEquivalentTo(
            [
                (utilities.Id, "Utilities", CategoryKind.Expense),
                (salary.Id, "Salary", CategoryKind.Income),
            ]);
    }

    [Fact]
    public async Task ListCategoryGroups_WithNoGroup_ReturnsNothing()
    {
        IReadOnlyList<CategoryGroup> listed = await ListAsync();

        listed.Should().BeEmpty();
    }

    [Fact]
    public async Task ListCategoryGroups_WhenCalled_PassesTheCancellationTokenThrough()
    {
        using var cancellation = new CancellationTokenSource();

        await new ListCategoryGroups(_groups).ExecuteAsync(cancellation.Token);

        _groups.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
    }

    private CategoryGroup Seed(string name, CategoryKind kind)
    {
        CategoryGroup group = CategoryGroup.Create(Guid.CreateVersion7(), name, kind, Now);
        _groups.Seed(group);

        return group;
    }

    private Task<IReadOnlyList<CategoryGroup>> ListAsync() =>
        new ListCategoryGroups(_groups).ExecuteAsync(CancellationToken.None);
}
