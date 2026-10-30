using FinNavis.Application.CategoryGroups;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;

namespace FinNavis.Application.UnitTests.CategoryGroups;

public sealed class DeleteCategoryGroupTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 18, 0, 0, TimeSpan.Zero);

    private readonly FakeCategoryGroupRepository _groups = new();
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeUnitOfWork _unitOfWork;

    public DeleteCategoryGroupTests() => _unitOfWork = new FakeUnitOfWork(Commit);

    // No category is deleted: only the link is cleared, so each one keeps its id, its name
    // and its side — and with its id, every entry recorded against it.
    [Fact]
    public async Task DeleteCategoryGroup_StillHoldingCategories_ClearsLinksAndRemovesGroup()
    {
        CategoryGroup group = SeedGroup("Utilities", CategoryKind.Expense);
        Category water = SeedCategory("Water", CategoryKind.Expense, group);
        Category gas = SeedCategory("Gas", CategoryKind.Expense, group);
        Category rent = SeedCategory("Rent", CategoryKind.Expense, group: null);

        await DeleteAsync(group.Id);

        _groups.Stored.Should().BeEmpty();
        _categories.Stored.Select(category => (category.Id, category.Name, category.Kind))
            .Should().BeEquivalentTo(
            [
                (water.Id, "Water", CategoryKind.Expense),
                (gas.Id, "Gas", CategoryKind.Expense),
                (rent.Id, "Rent", CategoryKind.Expense),
            ]);

        // Every link is empty, and none of them points at a group called Default — there is no
        // such row. The breakdown produces that bucket at read time.
        _categories.CommittedGroupLinks.Values.Should().AllSatisfy(
            link => link.Should().BeNull());
    }

    // The clear and the removal are one unit of work. Two commits would leave a window where a
    // category still points at a group that is gone, which the NO ACTION foreign key would refuse.
    [Fact]
    public async Task DeleteCategoryGroup_StillHoldingCategories_CommitsOnce()
    {
        CategoryGroup group = SeedGroup("Utilities", CategoryKind.Expense);
        SeedCategory("Water", CategoryKind.Expense, group);
        SeedCategory("Gas", CategoryKind.Expense, group);

        await DeleteAsync(group.Id);

        _unitOfWork.Commits.Should().Be(1);
    }

    // A failure part way leaves the group and every link as they were. The entities in memory
    // already changed, exactly as EF Core's change tracking leaves them, so what the test reads is
    // the committed view — the only one PostgreSQL would have.
    [Fact]
    public async Task DeleteCategoryGroup_WhenTheCommitFails_LeavesTheGroupAndEveryLinkAsTheyWere()
    {
        CategoryGroup group = SeedGroup("Utilities", CategoryKind.Expense);
        Category water = SeedCategory("Water", CategoryKind.Expense, group);
        var failingUnitOfWork = new FakeUnitOfWork(() => throw new InvalidOperationException("commit failed"));

        Func<Task> delete = () => new DeleteCategoryGroup(_groups, _categories, failingUnitOfWork)
            .ExecuteAsync(group.Id, CancellationToken.None);

        await delete.Should().ThrowAsync<InvalidOperationException>();

        _groups.Stored.Should().ContainSingle().Which.Id.Should().Be(group.Id);
        _groups.CommittedNames[group.Id].Should().Be("Utilities");
        _categories.CommittedGroupLinks[water.Id].Should().Be(group.Id);
    }

    [Fact]
    public async Task DeleteCategoryGroup_HoldingNoCategory_IsRemoved()
    {
        CategoryGroup group = SeedGroup("Travel", CategoryKind.Expense);

        await DeleteAsync(group.Id);

        _groups.Stored.Should().BeEmpty();
        _unitOfWork.Commits.Should().Be(1);
    }

    // A group on the other side keeps its categories: only the categories of the removed group
    // are unlinked.
    [Fact]
    public async Task DeleteCategoryGroup_WithAnotherGroupInTheLedger_LeavesTheOtherGroupUntouched()
    {
        CategoryGroup removed = SeedGroup("Utilities", CategoryKind.Expense);
        CategoryGroup kept = SeedGroup("Salary", CategoryKind.Income);
        SeedCategory("Water", CategoryKind.Expense, removed);
        Category bonus = SeedCategory("Bonus", CategoryKind.Income, kept);

        await DeleteAsync(removed.Id);

        _groups.Stored.Should().ContainSingle().Which.Id.Should().Be(kept.Id);
        _categories.CommittedGroupLinks[bonus.Id].Should().Be(kept.Id);
    }

    [Fact]
    public async Task DeleteCategoryGroup_UnknownGroupId_IsRefusedAsNotFound()
    {
        Guid unknownId = Guid.CreateVersion7();

        Func<Task> delete = () => DeleteAsync(unknownId);

        var refusal = (await delete.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("CategoryGroup.NotFound");
        refusal.Message.Should().Be($"No category group in the ledger has id {unknownId}.");
        _unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task DeleteCategoryGroup_WhenCalled_PassesTheCancellationTokenThrough()
    {
        CategoryGroup group = SeedGroup("Utilities", CategoryKind.Expense);
        SeedCategory("Water", CategoryKind.Expense, group);
        using var cancellation = new CancellationTokenSource();

        await new DeleteCategoryGroup(_groups, _categories, _unitOfWork)
            .ExecuteAsync(group.Id, cancellation.Token);

        _groups.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
        _categories.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
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

    private Category SeedCategory(string name, CategoryKind kind, CategoryGroup? group)
    {
        Category category = Category.Create(Guid.CreateVersion7(), name, kind, group, Now);
        _categories.Seed(category);

        return category;
    }

    private Task DeleteAsync(Guid categoryGroupId) =>
        new DeleteCategoryGroup(_groups, _categories, _unitOfWork)
            .ExecuteAsync(categoryGroupId, CancellationToken.None);
}
