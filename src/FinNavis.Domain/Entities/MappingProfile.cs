using FinNavis.Domain.Errors;

namespace FinNavis.Domain.Entities;

public sealed class MappingProfile
{
    public const int MaxShapeKeyLength = 1000;

    private readonly List<MappingRule> _rules = [];

    private MappingProfile(Guid id, string shapeKey, DateTimeOffset createdAt)
    {
        Id = id;
        ShapeKey = shapeKey;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string ShapeKey { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<MappingRule> Rules => _rules;

    public static MappingProfile Create(
        Guid id,
        string shapeKey,
        IReadOnlyCollection<MappingRule> rules,
        DateTimeOffset createdAt)
    {
        RequireValidShapeKey(shapeKey);

        MappingProfile profile = new(id, shapeKey, createdAt);
        profile.ReplaceRules(rules);

        return profile;
    }

    public void AddRule(MappingRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        if (_rules.Exists(existing => existing.SourceCategory == rule.SourceCategory))
        {
            throw DuplicateSourceCategory();
        }

        rule.BelongTo(Id);
        _rules.Add(rule);
    }

    // Every new rule is checked before any old one goes, so a refused replacement leaves the
    // profile with the rules it had.
    public void ReplaceRules(IReadOnlyCollection<MappingRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        bool namesASourceCategoryTwice =
            rules.Select(rule => rule.SourceCategory).Distinct(StringComparer.Ordinal).Count() != rules.Count;

        if (namesASourceCategoryTwice)
        {
            throw DuplicateSourceCategory();
        }

        _rules.Clear();

        foreach (MappingRule rule in rules)
        {
            AddRule(rule);
        }
    }

    private static DomainRuleViolationException DuplicateSourceCategory() =>
        new(
            "MappingProfile.DuplicateSourceCategory",
            "This file category is named by two rules, so it is not clear which one applies.");

    // The shape key is already the normalised header row, so it is checked and never reshaped:
    // changing it here would make it stop matching the next file of the same shape.
    private static void RequireValidShapeKey(string shapeKey)
    {
        if (string.IsNullOrWhiteSpace(shapeKey) || shapeKey.Length > MaxShapeKeyLength)
        {
            throw new DomainRuleViolationException(
                "MappingProfile.ShapeKeyInvalid",
                "A file header must be between 1 and 1000 characters.");
        }
    }
}
