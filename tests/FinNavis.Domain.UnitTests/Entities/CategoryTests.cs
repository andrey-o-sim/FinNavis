using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;

namespace FinNavis.Domain.UnitTests.Entities;

public sealed class CategoryTests
{
    private const string NameInvalid = "Category.NameInvalid";
    private const string GroupKindMismatch = "Category.GroupKindMismatch";

    private static readonly DateTimeOffset CreatedAt =
        new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithNoGroup_SitsInNoGroup()
    {
        Category category = Category.Create(
            Guid.CreateVersion7(), "Groceries", CategoryKind.Expense, group: null, CreatedAt);

        category.Name.Should().Be("Groceries");
        category.Kind.Should().Be(CategoryKind.Expense);
        category.CategoryGroupId.Should().BeNull();
        category.CreatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void Create_WithGroupOnTheSameSide_SitsInThatGroup()
    {
        CategoryGroup group = Group(CategoryKind.Expense);

        Category category = Category.Create(
            Guid.CreateVersion7(), "Groceries", CategoryKind.Expense, group, CreatedAt);

        category.CategoryGroupId.Should().Be(group.Id);
    }

    [Fact]
    public void Create_WithGroupOnTheOtherSide_IsRefused()
    {
        CategoryGroup group = Group(CategoryKind.Income);

        Func<Category> act = () => Category.Create(
            Guid.CreateVersion7(), "Groceries", CategoryKind.Expense, group, CreatedAt);

        act.Should().Throw<DomainRuleViolationException>()
            .Which.Code.Should().Be(GroupKindMismatch);
    }

    [Fact]
    public void Create_NameWithSurroundingSpaces_IsTrimmed()
    {
        Category category = Category.Create(
            Guid.CreateVersion7(), "  Groceries  ", CategoryKind.Expense, group: null, CreatedAt);

        category.Name.Should().Be("Groceries");
    }

    [Fact]
    public void Create_NameOfExactlyHundredCharacters_IsAccepted()
    {
        string name = new('a', Category.MaxNameLength);

        Category category = Category.Create(
            Guid.CreateVersion7(), name, CategoryKind.Income, group: null, CreatedAt);

        category.Name.Should().Be(name);
    }

    [Fact]
    public void Create_NameOverHundredCharacters_IsRefused()
    {
        string name = new('a', Category.MaxNameLength + 1);

        Func<Category> act = () => Category.Create(
            Guid.CreateVersion7(), name, CategoryKind.Expense, group: null, CreatedAt);

        act.Should().Throw<DomainRuleViolationException>()
            .Which.Code.Should().Be(NameInvalid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Create_BlankName_IsRefused(string name)
    {
        Func<Category> act = () => Category.Create(
            Guid.CreateVersion7(), name, CategoryKind.Expense, group: null, CreatedAt);

        act.Should().Throw<DomainRuleViolationException>()
            .Which.Code.Should().Be(NameInvalid);
    }

    [Fact]
    public void Rename_ValidName_KeepsSameIdentity()
    {
        Guid id = Guid.CreateVersion7();
        CategoryGroup group = Group(CategoryKind.Expense);
        Category category = Category.Create(id, "Groceries", CategoryKind.Expense, group, CreatedAt);

        category.Rename("Food");

        // AC-34: a transaction names its category by id, so every recorded movement, balance and
        // breakdown follows the new name with nothing rewritten.
        category.Id.Should().Be(id);
        category.Name.Should().Be("Food");
        category.Kind.Should().Be(CategoryKind.Expense);
        category.CategoryGroupId.Should().Be(group.Id);
        category.CreatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void Rename_NameOverHundredCharacters_IsRefusedAndKeepsTheOldName()
    {
        Category category = Category.Create(
            Guid.CreateVersion7(), "Groceries", CategoryKind.Expense, group: null, CreatedAt);

        Action act = () => category.Rename(new string('a', Category.MaxNameLength + 1));

        act.Should().Throw<DomainRuleViolationException>()
            .Which.Code.Should().Be(NameInvalid);
        category.Name.Should().Be("Groceries");
    }

    [Fact]
    public void Rename_BlankName_IsRefusedAndKeepsTheOldName()
    {
        Category category = Category.Create(
            Guid.CreateVersion7(), "Groceries", CategoryKind.Expense, group: null, CreatedAt);

        Action act = () => category.Rename("   ");

        act.Should().Throw<DomainRuleViolationException>()
            .Which.Code.Should().Be(NameInvalid);
        category.Name.Should().Be("Groceries");
    }

    [Fact]
    public void PlaceInGroup_GroupOnTheSameSide_SitsInThatGroup()
    {
        Category category = Category.Create(
            Guid.CreateVersion7(), "Groceries", CategoryKind.Expense, group: null, CreatedAt);
        CategoryGroup group = Group(CategoryKind.Expense);

        category.PlaceInGroup(group);

        category.CategoryGroupId.Should().Be(group.Id);
    }

    [Fact]
    public void PlaceInGroup_GroupOnOtherSide_IsRefused()
    {
        Category category = Category.Create(
            Guid.CreateVersion7(), "Groceries", CategoryKind.Expense, group: null, CreatedAt);
        CategoryGroup group = Group(CategoryKind.Income);

        Action act = () => category.PlaceInGroup(group);

        // AC-30: the entity refuses, not the schema (data-model.md decision 7), so the owner reads
        // the acceptance criterion's wording instead of a foreign key violation.
        DomainRuleViolationException refusal = act.Should()
            .Throw<DomainRuleViolationException>().Which;
        refusal.Code.Should().Be(GroupKindMismatch);
        refusal.Message.Should()
            .Be("An expense category cannot sit in an income category group.");
        category.CategoryGroupId.Should().BeNull();
    }

    [Fact]
    public void PlaceInGroup_ExpenseGroupForAnIncomeCategory_IsRefusedWithBothSidesNamed()
    {
        Category category = Category.Create(
            Guid.CreateVersion7(), "Salary", CategoryKind.Income, group: null, CreatedAt);
        CategoryGroup group = Group(CategoryKind.Expense);

        Action act = () => category.PlaceInGroup(group);

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("An income category cannot sit in an expense category group.");
    }

    [Fact]
    public void MoveToGroup_CategoryWithHistory_KeepsSameIdentity()
    {
        // AC-13. The move is PlaceInGroup — placing a category and moving one between groups are
        // the same change to the same row.
        Guid id = Guid.CreateVersion7();
        CategoryGroup from = Group(CategoryKind.Expense);
        CategoryGroup to = Group(CategoryKind.Expense);
        Category category = Category.Create(id, "Groceries", CategoryKind.Expense, from, CreatedAt);

        category.PlaceInGroup(to);

        // Only the group link changed. A transaction names its category and never its group, so no
        // transaction row is rewritten and the figure for a closed period cannot change.
        category.Id.Should().Be(id);
        category.Name.Should().Be("Groceries");
        category.Kind.Should().Be(CategoryKind.Expense);
        category.CreatedAt.Should().Be(CreatedAt);
        category.CategoryGroupId.Should().Be(to.Id);
    }

    [Fact]
    public void ClearGroup_CategoryKeepsIdentityAndKind()
    {
        // AC-29, the domain half. The group is removed, the category stays with all of its
        // history, and the breakdown reports it under the Default group from then on.
        Guid id = Guid.CreateVersion7();
        CategoryGroup group = Group(CategoryKind.Expense);
        Category category = Category.Create(id, "Groceries", CategoryKind.Expense, group, CreatedAt);

        category.ClearGroup();

        category.Id.Should().Be(id);
        category.Name.Should().Be("Groceries");
        category.Kind.Should().Be(CategoryKind.Expense);
        category.CreatedAt.Should().Be(CreatedAt);

        // No Default group is assigned. It is produced by the breakdown and never stored
        // (sad.md §12, data-model.md decision 8), so the category simply has no group.
        category.CategoryGroupId.Should().BeNull();
    }

    [Fact]
    public void ClearGroup_CategoryAlreadyInNoGroup_StaysInNoGroup()
    {
        Category category = Category.Create(
            Guid.CreateVersion7(), "Groceries", CategoryKind.Expense, group: null, CreatedAt);

        category.ClearGroup();

        category.CategoryGroupId.Should().BeNull();
    }

    private static CategoryGroup Group(CategoryKind kind) =>
        CategoryGroup.Create(Guid.CreateVersion7(), "Utilities", kind, CreatedAt);
}
