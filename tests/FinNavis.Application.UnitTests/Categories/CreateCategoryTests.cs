using FinNavis.Application.Categories;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;

namespace FinNavis.Application.UnitTests.Categories;

public sealed class CreateCategoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 18, 0, 0, TimeSpan.Zero);

    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeCategoryGroupRepository _groups = new();
    private readonly FakeUnitOfWork _unitOfWork;

    public CreateCategoryTests() => _unitOfWork = new FakeUnitOfWork(_categories.Commit);

    // Both shapes are allowed, and both become available to file a transaction under.
    [Fact]
    public async Task CreateCategory_WithAndWithoutGroup_BecomesAvailableToName()
    {
        CategoryGroup utilities = SeedGroup("Utilities", CategoryKind.Expense);

        Category grouped = await CreateAsync("Water", CategoryKind.Expense, utilities.Id);
        Category loose = await CreateAsync("Rent", CategoryKind.Expense, categoryGroupId: null);

        grouped.CategoryGroupId.Should().Be(utilities.Id);
        grouped.CreatedAt.Should().Be(Now);

        // Not a group called Default — the link is simply empty.
        loose.CategoryGroupId.Should().BeNull();

        _categories.Stored.Select(category => category.Id)
            .Should().BeEquivalentTo([grouped.Id, loose.Id]);
    }

    // The group is named back and it is not created on the way.
    [Fact]
    public async Task CreateCategory_UnknownGroup_IsRefusedAndCreatesNothing()
    {
        Guid unknownGroupId = Guid.CreateVersion7();

        Func<Task> create = () => CreateAsync("Water", CategoryKind.Expense, unknownGroupId);

        var refusal = (await create.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("CategoryGroup.NotFound");
        refusal.Message.Should().Be($"No category group in the ledger has id {unknownGroupId}.");
        _categories.Stored.Should().BeEmpty();
        _groups.Stored.Should().BeEmpty();
        _unitOfWork.Commits.Should().Be(0);
    }

    // On the create side the entity owns this refusal — it is the one rule a category can check
    // on its own, because both sides are in front of it.
    [Fact]
    public async Task PlaceCategory_GroupOnOtherSide_IsRefused()
    {
        CategoryGroup earning = SeedGroup("Salary", CategoryKind.Income);

        Func<Task> create = () => CreateAsync("Water", CategoryKind.Expense, earning.Id);

        var refusal = (await create.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Category.GroupKindMismatch");
        refusal.Message.Should().Be("An expense category cannot sit in an income category group.");
        _categories.Stored.Should().BeEmpty();
        _unitOfWork.Commits.Should().Be(0);
    }

    // The check is scoped exactly as ux_categories_name_kind is, and the index stays the last
    // guard — this refusal is only what gives the owner the wording.
    [Fact]
    public async Task CreateCategory_NameTakenOnSameSide_IsRefused()
    {
        await CreateAsync("Groceries", CategoryKind.Expense, categoryGroupId: null);

        Func<Task> create = () => CreateAsync("Groceries", CategoryKind.Expense, categoryGroupId: null);

        var refusal = (await create.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Category.DuplicateName");
        refusal.Message.Should().Be("An expense category named 'Groceries' already exists.");
        _categories.Stored.Should().ContainSingle();
    }

    // The other half: the name stays free on the other side of the ledger.
    [Fact]
    public async Task CreateCategory_SameNameOnTheOtherSide_IsAccepted()
    {
        Category spending = await CreateAsync("Gifts", CategoryKind.Expense, categoryGroupId: null);
        Category earning = await CreateAsync("Gifts", CategoryKind.Income, categoryGroupId: null);

        earning.Id.Should().NotBe(spending.Id);
        _categories.Stored.Select(category => category.Kind)
            .Should().BeEquivalentTo([CategoryKind.Expense, CategoryKind.Income]);
    }

    [Fact]
    public async Task CreateCategory_WithABlankName_IsRefusedAndWritesNothing()
    {
        Func<Task> create = () => CreateAsync("   ", CategoryKind.Expense, categoryGroupId: null);

        (await create.Should().ThrowAsync<DomainRuleViolationException>())
            .Which.Code.Should().Be("Category.NameInvalid");

        _categories.Stored.Should().BeEmpty();
        _unitOfWork.Commits.Should().Be(0);
    }

    // The id is minted here, in the Application layer, and it is a UUID v7.
    [Fact]
    public async Task CreateCategory_NewName_MintsATimeOrderedUuidV7()
    {
        Category first = await CreateAsync("Water", CategoryKind.Expense, categoryGroupId: null);
        Category second = await CreateAsync("Gas", CategoryKind.Expense, categoryGroupId: null);

        first.Id.Version.Should().Be(7);
        second.Id.Version.Should().Be(7);
        second.Id.Should().NotBe(first.Id);
    }

    [Fact]
    public async Task CreateCategory_WhenCalled_PassesTheCancellationTokenThrough()
    {
        CategoryGroup utilities = SeedGroup("Utilities", CategoryKind.Expense);
        using var cancellation = new CancellationTokenSource();

        await new CreateCategory(_categories, _groups, _unitOfWork, new FixedClock(Now))
            .ExecuteAsync("Water", CategoryKind.Expense, utilities.Id, cancellation.Token);

        _categories.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
        _groups.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
        _unitOfWork.TokensSeen.Should().AllSatisfy(token => token.Should().Be(cancellation.Token));
    }

    private CategoryGroup SeedGroup(string name, CategoryKind kind)
    {
        CategoryGroup group = CategoryGroup.Create(Guid.CreateVersion7(), name, kind, Now);
        _groups.Seed(group);

        return group;
    }

    private Task<Category> CreateAsync(string name, CategoryKind kind, Guid? categoryGroupId) =>
        new CreateCategory(_categories, _groups, _unitOfWork, new FixedClock(Now))
            .ExecuteAsync(name, kind, categoryGroupId, CancellationToken.None);
}
