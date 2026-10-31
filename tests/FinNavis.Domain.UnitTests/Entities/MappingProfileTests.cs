using System.Reflection;
using System.Runtime.CompilerServices;

using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;

namespace FinNavis.Domain.UnitTests.Entities;

public sealed class MappingProfileTests
{
    private const string InvalidRule = "MappingProfile.InvalidRule";
    private const string DuplicateSourceCategory = "MappingProfile.DuplicateSourceCategory";
    private const string ShapeKeyInvalid = "MappingProfile.ShapeKeyInvalid";

    private const string ShapeKey = "date|description|category|amount";

    private const BindingFlags EveryMember =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    private static readonly DateTimeOffset CreatedAt =
        new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ValidShapeKeyAndRules_KeepsThemAndOwnsEveryRule()
    {
        Guid id = Guid.CreateVersion7();
        MappingRule groceries = CategoryRule("TEST GROCERIES");
        MappingRule transfer = TransferRule("TEST TRANSFER");

        MappingProfile profile = MappingProfile.Create(id, ShapeKey, [groceries, transfer], CreatedAt);

        profile.Id.Should().Be(id);
        profile.ShapeKey.Should().Be(ShapeKey);
        profile.CreatedAt.Should().Be(CreatedAt);
        profile.Rules.Should().Equal(groceries, transfer);
        profile.Rules.Should().OnlyContain(rule => rule.MappingProfileId == id);
    }

    [Fact]
    public void Create_ShapeKeyOfExactlyThousandCharacters_IsAccepted()
    {
        string shapeKey = new('a', MappingProfile.MaxShapeKeyLength);

        MappingProfile profile = MappingProfile.Create(Guid.CreateVersion7(), shapeKey, [], CreatedAt);

        profile.ShapeKey.Should().Be(shapeKey);
    }

    [Fact]
    public void Create_ShapeKeyOverThousandCharacters_IsRefused()
    {
        string shapeKey = new('a', MappingProfile.MaxShapeKeyLength + 1);

        Func<MappingProfile> act = () => MappingProfile.Create(Guid.CreateVersion7(), shapeKey, [], CreatedAt);

        act.Should().Throw<DomainRuleViolationException>()
            .Which.Code.Should().Be(ShapeKeyInvalid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Create_BlankShapeKey_IsRefused(string shapeKey)
    {
        Func<MappingProfile> act = () => MappingProfile.Create(Guid.CreateVersion7(), shapeKey, [], CreatedAt);

        act.Should().Throw<DomainRuleViolationException>()
            .Which.Code.Should().Be(ShapeKeyInvalid);
    }

    [Fact]
    public void Create_TwoRulesForTheSameSourceCategory_IsRefused()
    {
        Func<MappingProfile> act = () => MappingProfile.Create(
            Guid.CreateVersion7(),
            ShapeKey,
            [CategoryRule("TEST GROCERIES"), TransferRule("TEST GROCERIES")],
            CreatedAt);

        DomainRuleViolationException refusal = act.Should()
            .Throw<DomainRuleViolationException>().Which;
        refusal.Code.Should().Be(DuplicateSourceCategory);
        refusal.Message.Should().Be(
            "This file category is named by two rules, so it is not clear which one applies.");
    }

    [Fact]
    public void AddRule_SourceCategoryTheProfileAlreadyCovers_IsRefusedAndKeepsTheRules()
    {
        MappingRule groceries = CategoryRule("TEST GROCERIES");
        MappingProfile profile = MappingProfile.Create(Guid.CreateVersion7(), ShapeKey, [groceries], CreatedAt);

        Action act = () => profile.AddRule(TransferRule("TEST GROCERIES"));

        act.Should().Throw<DomainRuleViolationException>()
            .Which.Code.Should().Be(DuplicateSourceCategory);
        profile.Rules.Should().Equal(groceries);
    }

    [Fact]
    public void AddRule_SourceCategoryDifferingOnlyInCase_IsAccepted()
    {
        // The key is the text exactly as the bank writes it. Two spellings are two file
        // categories, because the next file of this shape writes each one the same way again.
        MappingProfile profile = MappingProfile.Create(
            Guid.CreateVersion7(), ShapeKey, [CategoryRule("TEST GROCERIES")], CreatedAt);

        profile.AddRule(CategoryRule("Test Groceries"));

        profile.Rules.Select(rule => rule.SourceCategory)
            .Should().Equal("TEST GROCERIES", "Test Groceries");
    }

    [Fact]
    public void ReplaceRules_NewRules_KeepsIdAndShapeKeyAndDropsEveryOldRule()
    {
        Guid id = Guid.CreateVersion7();
        MappingRule oldGroceries = CategoryRule("TEST GROCERIES");
        MappingRule oldTransfer = TransferRule("TEST TRANSFER");
        MappingProfile profile = MappingProfile.Create(id, ShapeKey, [oldGroceries, oldTransfer], CreatedAt);
        MappingRule newGroceries = TransferRule("TEST GROCERIES");
        MappingRule newRent = CategoryRule("TEST RENT");

        profile.ReplaceRules([newGroceries, newRent]);

        profile.Id.Should().Be(id);
        profile.ShapeKey.Should().Be(ShapeKey);
        profile.CreatedAt.Should().Be(CreatedAt);
        profile.Rules.Should().Equal(newGroceries, newRent);
        profile.Rules.Should().NotContain(oldGroceries).And.NotContain(oldTransfer);
        profile.Rules.Should().OnlyContain(rule => rule.MappingProfileId == id);
    }

    [Fact]
    public void ReplaceRules_TwoRulesForTheSameSourceCategory_IsRefusedAndKeepsTheOldRules()
    {
        MappingRule groceries = CategoryRule("TEST GROCERIES");
        MappingProfile profile = MappingProfile.Create(Guid.CreateVersion7(), ShapeKey, [groceries], CreatedAt);

        Action act = () => profile.ReplaceRules([CategoryRule("TEST RENT"), TransferRule("TEST RENT")]);

        act.Should().Throw<DomainRuleViolationException>()
            .Which.Code.Should().Be(DuplicateSourceCategory);
        profile.Rules.Should().Equal(groceries);
    }

    [Fact]
    public void ReplaceRules_NoRules_LeavesTheProfileEmpty()
    {
        MappingProfile profile = MappingProfile.Create(
            Guid.CreateVersion7(), ShapeKey, [CategoryRule("TEST GROCERIES")], CreatedAt);

        profile.ReplaceRules([]);

        profile.Rules.Should().BeEmpty();
    }

    [Fact]
    public void CreateRule_CategoryTargetWithACategory_KeepsEveryField()
    {
        Guid id = Guid.CreateVersion7();
        Guid categoryId = Guid.CreateVersion7();

        MappingRule rule = MappingRule.Create(
            id, "TEST GROCERIES", MappingRuleTarget.Category, categoryId, CreatedAt);

        rule.Id.Should().Be(id);
        rule.SourceCategory.Should().Be("TEST GROCERIES");
        rule.Target.Should().Be(MappingRuleTarget.Category);
        rule.TargetCategoryId.Should().Be(categoryId);
        rule.CreatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void CreateRule_TransferTargetWithNoCategory_IsAccepted()
    {
        MappingRule rule = TransferRule("TEST TRANSFER");

        rule.Target.Should().Be(MappingRuleTarget.TransferBetweenOwnAccounts);
        rule.TargetCategoryId.Should().BeNull();
    }

    [Fact]
    public void CreateRule_CategoryTargetWithNoCategory_IsRefused()
    {
        Func<MappingRule> act = () => MappingRule.Create(
            Guid.CreateVersion7(), "TEST GROCERIES", MappingRuleTarget.Category, null, CreatedAt);

        DomainRuleViolationException refusal = act.Should()
            .Throw<DomainRuleViolationException>().Which;
        refusal.Code.Should().Be(InvalidRule);
        refusal.Message.Should().Be("A rule that files a row under a category must name that category.");
    }

    [Fact]
    public void CreateRule_TransferTargetWithACategory_IsRefused()
    {
        Func<MappingRule> act = () => MappingRule.Create(
            Guid.CreateVersion7(),
            "TEST TRANSFER",
            MappingRuleTarget.TransferBetweenOwnAccounts,
            Guid.CreateVersion7(),
            CreatedAt);

        DomainRuleViolationException refusal = act.Should()
            .Throw<DomainRuleViolationException>().Which;
        refusal.Code.Should().Be(InvalidRule);
        refusal.Message.Should().Be("A rule for a move between your own accounts records no category.");
    }

    [Fact]
    public void CreateRule_SourceCategoryWithSurroundingSpaces_IsKeptAsTheFileWritesIt()
    {
        MappingRule rule = CategoryRule("  TEST GROCERIES ");

        rule.SourceCategory.Should().Be("  TEST GROCERIES ");
    }

    [Fact]
    public void CreateRule_SourceCategoryOfExactlyTwoHundredCharacters_IsAccepted()
    {
        string sourceCategory = new('a', MappingRule.MaxSourceCategoryLength);

        MappingRule rule = CategoryRule(sourceCategory);

        rule.SourceCategory.Should().Be(sourceCategory);
    }

    [Fact]
    public void CreateRule_SourceCategoryOverTwoHundredCharacters_IsRefused()
    {
        Func<MappingRule> act = () => CategoryRule(new string('a', MappingRule.MaxSourceCategoryLength + 1));

        act.Should().Throw<DomainRuleViolationException>()
            .Which.Code.Should().Be(InvalidRule);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateRule_BlankSourceCategory_IsRefused(string sourceCategory)
    {
        Func<MappingRule> act = () => CategoryRule(sourceCategory);

        act.Should().Throw<DomainRuleViolationException>()
            .Which.Code.Should().Be(InvalidRule);
    }

    [Theory]
    [InlineData(typeof(Transaction))]
    [InlineData(typeof(Transfer))]
    public void RecordedMovement_HasNoWayToPointBackAtARule(Type movementType)
    {
        // Changing a rule leaves every recorded row as it was only because nothing recorded knows
        // a rule exists. A MappingRuleId added to a movement for convenience would break that, and
        // no behavioural test would notice.
        MemberInfo[] members = movementType.GetMembers(EveryMember);

        members.Should().NotContain(member =>
            member.Name.Contains("MappingRule", StringComparison.OrdinalIgnoreCase)
            || member.Name.Contains("MappingProfile", StringComparison.OrdinalIgnoreCase)
            || HoldsARule(member));
    }

    [Fact]
    public void Domain_NoTypeButTheProfileHoldsARule()
    {
        Type[] holders = typeof(MappingRule).Assembly
            .GetTypes()
            .Where(type => type != typeof(MappingRule) && type != typeof(MappingProfile))
            .Where(type => !type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
            .Where(type => type.GetFields(EveryMember).Any(HoldsARule))
            .ToArray();

        holders.Should().BeEmpty();
    }

    private static bool HoldsARule(MemberInfo member)
    {
        Type? memberType = member switch
        {
            FieldInfo field => field.FieldType,
            PropertyInfo property => property.PropertyType,
            _ => null,
        };

        return memberType is not null && MentionsARuleType(memberType);
    }

    private static bool MentionsARuleType(Type type) =>
        type == typeof(MappingRule)
        || type == typeof(MappingProfile)
        || (type.HasElementType && MentionsARuleType(type.GetElementType()!))
        || type.GenericTypeArguments.Any(MentionsARuleType);

    private static MappingRule CategoryRule(string sourceCategory) =>
        MappingRule.Create(
            Guid.CreateVersion7(), sourceCategory, MappingRuleTarget.Category, Guid.CreateVersion7(), CreatedAt);

    private static MappingRule TransferRule(string sourceCategory) =>
        MappingRule.Create(
            Guid.CreateVersion7(), sourceCategory, MappingRuleTarget.TransferBetweenOwnAccounts, null, CreatedAt);
}
