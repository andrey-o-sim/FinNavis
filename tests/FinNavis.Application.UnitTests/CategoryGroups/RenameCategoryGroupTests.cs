using FinNavis.Application.CategoryGroups;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;

namespace FinNavis.Application.UnitTests.CategoryGroups;

public sealed class RenameCategoryGroupTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 18, 0, 0, TimeSpan.Zero);

    private readonly FakeCategoryGroupRepository _groups = new();
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeUnitOfWork _unitOfWork;

    public RenameCategoryGroupTests() => _unitOfWork = new FakeUnitOfWork(Commit);

    // AC-28. A transaction names its category by id and a category names its group by id, so a
    // group's new name reaches the breakdown without a single category row being rewritten. That
    // is why every category figure is unchanged: the ids the figures are summed over did not move.
    [Fact]
    public async Task RenameCategoryGroup_GroupWithCategories_KeepsEveryCategoryFigure()
    {
        CategoryGroup group = SeedGroup("Utilities", CategoryKind.Expense);
        Category water = SeedCategory("Water", CategoryKind.Expense, group);
        Category gas = SeedCategory("Gas", CategoryKind.Expense, group);

        CategoryGroup renamed = await RenameAsync(group.Id, "Household bills");

        renamed.Id.Should().Be(group.Id);
        renamed.Name.Should().Be("Household bills");
        renamed.Kind.Should().Be(CategoryKind.Expense);
        _groups.CommittedNames[group.Id].Should().Be("Household bills");

        _categories.Stored.Select(category => (category.Id, category.Name, category.CategoryGroupId))
            .Should().BeEquivalentTo(
            [
                (water.Id, "Water", (Guid?)group.Id),
                (gas.Id, "Gas", (Guid?)group.Id),
            ]);
    }

    // AC-31, on the rename side.
    [Fact]
    public async Task RenameCategoryGroup_NameTakenOnSameSide_IsRefused()
    {
        CategoryGroup utilities = SeedGroup("Utilities", CategoryKind.Expense);
        SeedGroup("Travel", CategoryKind.Expense);

        Func<Task> rename = () => RenameAsync(utilities.Id, "Travel");

        var refusal = (await rename.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("CategoryGroup.DuplicateName");
        refusal.Message.Should().Be("An expense category group named 'Travel' already exists.");
        _groups.CommittedNames[utilities.Id].Should().Be("Utilities");
        _unitOfWork.Commits.Should().Be(0);
    }

    // The other half of AC-31: the same name on the other side belongs to another group.
    [Fact]
    public async Task RenameCategoryGroup_NameTakenOnTheOtherSide_IsAccepted()
    {
        CategoryGroup spending = SeedGroup("Utilities", CategoryKind.Expense);
        SeedGroup("Gifts", CategoryKind.Income);

        CategoryGroup renamed = await RenameAsync(spending.Id, "Gifts");

        renamed.Name.Should().Be("Gifts");
        renamed.Kind.Should().Be(CategoryKind.Expense);
    }

    // ExistsWithNameAsync answers true for the group's own row, so keeping the current name must
    // not read as a clash.
    [Fact]
    public async Task RenameCategoryGroup_ToItsOwnCurrentName_IsAccepted()
    {
        CategoryGroup group = SeedGroup("Utilities", CategoryKind.Expense);

        CategoryGroup renamed = await RenameAsync(group.Id, "Utilities");

        renamed.Name.Should().Be("Utilities");
        _unitOfWork.Commits.Should().Be(1);
    }

    [Fact]
    public async Task RenameCategoryGroup_UnknownGroupId_IsRefusedAsNotFound()
    {
        Guid unknownId = Guid.CreateVersion7();

        Func<Task> rename = () => RenameAsync(unknownId, "Utilities");

        var refusal = (await rename.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("CategoryGroup.NotFound");
        refusal.Message.Should().Be($"No category group in the ledger has id {unknownId}.");
        _unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task RenameCategoryGroup_WithABlankName_IsRefusedAndLeavesTheNameAsItWas()
    {
        CategoryGroup group = SeedGroup("Utilities", CategoryKind.Expense);

        Func<Task> rename = () => RenameAsync(group.Id, "   ");

        (await rename.Should().ThrowAsync<DomainRuleViolationException>())
            .Which.Code.Should().Be("CategoryGroup.NameInvalid");

        _groups.CommittedNames[group.Id].Should().Be("Utilities");
        _unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task RenameCategoryGroup_WhenCalled_PassesTheCancellationTokenThrough()
    {
        CategoryGroup group = SeedGroup("Utilities", CategoryKind.Expense);
        using var cancellation = new CancellationTokenSource();

        await new RenameCategoryGroup(_groups, _unitOfWork)
            .ExecuteAsync(group.Id, "Household bills", cancellation.Token);

        _groups.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
        _unitOfWork.TokensSeen.Should().AllSatisfy(token => token.Should().Be(cancellation.Token));
    }

    private void Commit()
    {
        _groups.Commit();
        _categories.Commit();
    }

    private CategoryGroup SeedGroup(string name, CategoryKind kind)
    {
        CategoryGroup group = CategoryGroup.Create(Guid.CreateVersion7(), name, kind, Now);
        _groups.Seed(group);

        return group;
    }

    private Category SeedCategory(string name, CategoryKind kind, CategoryGroup group)
    {
        Category category = Category.Create(Guid.CreateVersion7(), name, kind, group, Now);
        _categories.Seed(category);

        return category;
    }

    private Task<CategoryGroup> RenameAsync(Guid categoryGroupId, string name) =>
        new RenameCategoryGroup(_groups, _unitOfWork)
            .ExecuteAsync(categoryGroupId, name, CancellationToken.None);
}
