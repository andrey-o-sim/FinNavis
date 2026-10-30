using FinNavis.Application.CategoryGroups;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;

namespace FinNavis.Application.UnitTests.CategoryGroups;

public sealed class CreateCategoryGroupTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 18, 0, 0, TimeSpan.Zero);

    private readonly FakeCategoryGroupRepository _groups = new();
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeUnitOfWork _unitOfWork;

    public CreateCategoryGroupTests() => _unitOfWork = new FakeUnitOfWork(_groups.Commit);

    // Nothing is written to make the group empty: a category joins a group by pointing at
    // it, and no category can already point at an id minted a moment ago.
    [Fact]
    public async Task CreateCategoryGroup_NamedSide_CreatedEmpty()
    {
        CategoryGroup group = await CreateAsync("Utilities", CategoryKind.Expense);

        IReadOnlyList<Category> held = await _categories.ListByGroupAsync(group.Id, CancellationToken.None);

        group.Name.Should().Be("Utilities");
        group.Kind.Should().Be(CategoryKind.Expense);
        group.CreatedAt.Should().Be(Now);
        _groups.Stored.Should().ContainSingle();
        held.Should().BeEmpty();
    }

    // The check is scoped exactly as ux_category_groups_name_kind is, and the index stays the
    // last guard — this refusal is only what gives the owner the wording.
    [Fact]
    public async Task CreateCategoryGroup_NameTakenOnSameSide_IsRefused()
    {
        await CreateAsync("Gifts", CategoryKind.Expense);

        Func<Task> create = () => CreateAsync("Gifts", CategoryKind.Expense);

        var refusal = (await create.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("CategoryGroup.DuplicateName");
        refusal.Message.Should().Be("An expense category group named 'Gifts' already exists.");
        _groups.Stored.Should().ContainSingle();
        _unitOfWork.Commits.Should().Be(1);
    }

    // The other half: the name stays free on the other side of the ledger.
    [Fact]
    public async Task CreateCategoryGroup_SameNameOnTheOtherSide_IsAccepted()
    {
        CategoryGroup spending = await CreateAsync("Gifts", CategoryKind.Expense);
        CategoryGroup earning = await CreateAsync("Gifts", CategoryKind.Income);

        earning.Id.Should().NotBe(spending.Id);
        _groups.Stored.Should().HaveCount(2);
        _groups.Stored.Select(group => group.Kind)
            .Should().BeEquivalentTo([CategoryKind.Expense, CategoryKind.Income]);
    }

    // The stored name is trimmed, so the surrounding spaces must not slip past the check.
    [Fact]
    public async Task CreateCategoryGroup_NameTakenApartFromSurroundingSpaces_IsRefused()
    {
        await CreateAsync("Utilities", CategoryKind.Expense);

        Func<Task> create = () => CreateAsync("  Utilities  ", CategoryKind.Expense);

        (await create.Should().ThrowAsync<DomainRuleViolationException>())
            .Which.Code.Should().Be("CategoryGroup.DuplicateName");
    }

    [Fact]
    public async Task CreateCategoryGroup_WithABlankName_IsRefusedAndWritesNothing()
    {
        Func<Task> create = () => CreateAsync("   ", CategoryKind.Expense);

        (await create.Should().ThrowAsync<DomainRuleViolationException>())
            .Which.Code.Should().Be("CategoryGroup.NameInvalid");

        _groups.Stored.Should().BeEmpty();
        _unitOfWork.Commits.Should().Be(0);
    }

    // The breakdown produces a bucket under this name for every ungrouped category. A row of the
    // same name would show twice in the breakdown under one name, once with no id.
    [Theory]
    [InlineData("Default group")]
    [InlineData("  Default group  ")]
    [InlineData("default GROUP")]
    public async Task CreateCategoryGroup_NamedAfterTheBreakdownBucket_IsRefused(string name)
    {
        Func<Task> create = () => CreateAsync(name, CategoryKind.Expense);

        var refusal = (await create.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("CategoryGroup.NameReserved");
        refusal.Message.Should().Be(
            "'Default group' is the breakdown's own bucket, so a group cannot carry that name.");
        _groups.Stored.Should().BeEmpty();
        _unitOfWork.Commits.Should().Be(0);
    }

    // The id is minted here, in the Application layer, and it is a UUID v7.
    [Fact]
    public async Task CreateCategoryGroup_NewName_MintsATimeOrderedUuidV7()
    {
        CategoryGroup first = await CreateAsync("Utilities", CategoryKind.Expense);
        CategoryGroup second = await CreateAsync("Travel", CategoryKind.Expense);

        first.Id.Version.Should().Be(7);
        second.Id.Version.Should().Be(7);
        second.Id.Should().NotBe(first.Id);
    }

    [Fact]
    public async Task CreateCategoryGroup_WhenCalled_PassesTheCancellationTokenThrough()
    {
        using var cancellation = new CancellationTokenSource();

        await new CreateCategoryGroup(_groups, _unitOfWork, new FixedClock(Now))
            .ExecuteAsync("Utilities", CategoryKind.Expense, cancellation.Token);

        _groups.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
        _unitOfWork.TokensSeen.Should().AllSatisfy(token => token.Should().Be(cancellation.Token));
    }

    private Task<CategoryGroup> CreateAsync(string name, CategoryKind kind) =>
        new CreateCategoryGroup(_groups, _unitOfWork, new FixedClock(Now))
            .ExecuteAsync(name, kind, CancellationToken.None);
}
