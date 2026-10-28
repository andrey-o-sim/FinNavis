using System.Reflection;

using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;

namespace FinNavis.Domain.UnitTests.Entities;

public sealed class CategoryGroupTests
{
    private const string NameInvalid = "CategoryGroup.NameInvalid";

    private static readonly DateTimeOffset CreatedAt =
        new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ValidName_KeepsTheNameAndTheSide()
    {
        CategoryGroup group = CategoryGroup.Create(
            Guid.CreateVersion7(), "Utilities", CategoryKind.Expense, CreatedAt);

        group.Name.Should().Be("Utilities");
        group.Kind.Should().Be(CategoryKind.Expense);
        group.CreatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void Create_NameWithSurroundingSpaces_IsTrimmed()
    {
        CategoryGroup group = CategoryGroup.Create(
            Guid.CreateVersion7(), "  Utilities  ", CategoryKind.Expense, CreatedAt);

        group.Name.Should().Be("Utilities");
    }

    [Fact]
    public void Create_NameOfExactlyHundredCharacters_IsAccepted()
    {
        string name = new('a', CategoryGroup.MaxNameLength);

        CategoryGroup group = CategoryGroup.Create(
            Guid.CreateVersion7(), name, CategoryKind.Income, CreatedAt);

        group.Name.Should().Be(name);
    }

    [Fact]
    public void Create_NameOverHundredCharacters_IsRefused()
    {
        string name = new('a', CategoryGroup.MaxNameLength + 1);

        Func<CategoryGroup> act = () => CategoryGroup.Create(
            Guid.CreateVersion7(), name, CategoryKind.Expense, CreatedAt);

        act.Should().Throw<DomainRuleViolationException>()
            .Which.Code.Should().Be(NameInvalid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Create_BlankName_IsRefused(string name)
    {
        Func<CategoryGroup> act = () => CategoryGroup.Create(
            Guid.CreateVersion7(), name, CategoryKind.Expense, CreatedAt);

        act.Should().Throw<DomainRuleViolationException>()
            .Which.Code.Should().Be(NameInvalid);
    }

    [Fact]
    public void Rename_ValidName_KeepsSameIdentity()
    {
        Guid id = Guid.CreateVersion7();
        CategoryGroup group = CategoryGroup.Create(
            id, "Utilities", CategoryKind.Expense, CreatedAt);

        group.Rename("Home bills");

        // AC-28: a category points at its group by id, so the rename rewrites no category row and
        // no transaction row. The figure for every category in the group is unchanged.
        group.Id.Should().Be(id);
        group.Name.Should().Be("Home bills");
        group.Kind.Should().Be(CategoryKind.Expense);
        group.CreatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void Rename_NameOverHundredCharacters_IsRefusedAndKeepsTheOldName()
    {
        CategoryGroup group = CategoryGroup.Create(
            Guid.CreateVersion7(), "Utilities", CategoryKind.Expense, CreatedAt);

        Action act = () => group.Rename(new string('a', CategoryGroup.MaxNameLength + 1));

        act.Should().Throw<DomainRuleViolationException>()
            .Which.Code.Should().Be(NameInvalid);
        group.Name.Should().Be("Utilities");
    }

    [Fact]
    public void Rename_BlankName_IsRefusedAndKeepsTheOldName()
    {
        CategoryGroup group = CategoryGroup.Create(
            Guid.CreateVersion7(), "Utilities", CategoryKind.Expense, CreatedAt);

        Action act = () => group.Rename("   ");

        act.Should().Throw<DomainRuleViolationException>()
            .Which.Code.Should().Be(NameInvalid);
        group.Name.Should().Be("Utilities");
    }

    [Fact]
    public void CategoryGroup_HasNoOperationThatChangesTheSide()
    {
        // data-model.md decision 7: the entity owns the matching-sides rule, so moving a group to
        // the other side would leave every category in it on the wrong one.
        PropertyInfo kind = typeof(CategoryGroup).GetProperty(nameof(CategoryGroup.Kind))!;

        kind.SetMethod!.IsPublic.Should().BeFalse();
    }

    [Fact]
    public void Domain_CreatesNoDefaultCategoryGroup()
    {
        // sad.md §12 and data-model.md decision 8: the Default group is a bucket the breakdown
        // produces at read time. It is never a row and never an object. An implementer who creates
        // a real Default group has misread it.
        Type[] domainTypes = typeof(CategoryGroup).Assembly
            .GetTypes()
            .Where(type => type.Namespace?.StartsWith("FinNavis.Domain", StringComparison.Ordinal) == true)
            .ToArray();

        IEnumerable<string> names = domainTypes
            .SelectMany(type => type.GetMembers(
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Select(member => member.Name)
            .Concat(domainTypes.Select(type => type.Name));

        names.Should().NotContain(name => name.Contains("Default", StringComparison.OrdinalIgnoreCase));
    }
}
