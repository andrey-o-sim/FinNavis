using FinNavis.Application.Categories;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;

namespace FinNavis.Application.UnitTests.Categories;

public sealed class DeleteCategoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 18, 0, 0, TimeSpan.Zero);

    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeLedgerQueries _ledger = new();
    private readonly FakeUnitOfWork _unitOfWork;

    public DeleteCategoryTests() => _unitOfWork = new FakeUnitOfWork(_categories.Commit);

    // AC-12: the category cannot see its own history, so the question goes to the read path. The
    // foreign key carries no cascade, so the refusal is what keeps a recorded entry from being
    // taken away with the label.
    [Fact]
    public async Task DeleteCategory_NamedByATransaction_IsRefused()
    {
        Category groceries = SeedCategory("Groceries", CategoryKind.Expense);
        _ledger.CategoriesWithRecordedHistory.Add(groceries.Id);

        Func<Task> delete = () => DeleteAsync(groceries.Id);

        var refusal = (await delete.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Category.HasRecordedHistory");
        refusal.Message.Should().Be("Recorded history depends on this category, so it cannot be deleted.");
        _categories.Stored.Should().ContainSingle();
        _unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task DeleteCategory_NamedByNoTransaction_IsRemoved()
    {
        Category unused = SeedCategory("Spare label", CategoryKind.Expense);

        await DeleteAsync(unused.Id);

        _categories.Stored.Should().BeEmpty();
        _unitOfWork.Commits.Should().Be(1);
    }

    // A category sitting in a group is removed like any other — the group is not touched, and the
    // group is not what refuses (AC-29 is the other direction).
    [Fact]
    public async Task DeleteCategory_SittingInAGroup_IsRemoved()
    {
        CategoryGroup utilities = CategoryGroup.Create(
            Guid.CreateVersion7(),
            "Utilities",
            CategoryKind.Expense,
            Now);
        Category water = Category.Create(Guid.CreateVersion7(), "Water", CategoryKind.Expense, utilities, Now);
        _categories.Seed(water);

        await DeleteAsync(water.Id);

        _categories.Stored.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteCategory_UnknownCategoryId_IsRefusedAsNotFound()
    {
        Guid unknownId = Guid.CreateVersion7();

        Func<Task> delete = () => DeleteAsync(unknownId);

        var refusal = (await delete.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Category.NotFound");
        refusal.Message.Should().Be($"No category in the ledger has id {unknownId}.");
        _unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task DeleteCategory_WhenCalled_PassesTheCancellationTokenThrough()
    {
        Category unused = SeedCategory("Spare label", CategoryKind.Expense);
        using var cancellation = new CancellationTokenSource();

        await new DeleteCategory(_categories, _ledger, _unitOfWork)
            .ExecuteAsync(unused.Id, cancellation.Token);

        _categories.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
        _ledger.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
        _unitOfWork.TokensSeen.Should().AllSatisfy(token => token.Should().Be(cancellation.Token));
    }

    private Category SeedCategory(string name, CategoryKind kind)
    {
        Category category = Category.Create(Guid.CreateVersion7(), name, kind, group: null, Now);
        _categories.Seed(category);

        return category;
    }

    private Task DeleteAsync(Guid categoryId) =>
        new DeleteCategory(_categories, _ledger, _unitOfWork).ExecuteAsync(categoryId, CancellationToken.None);
}
