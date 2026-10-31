using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;

namespace FinNavis.Domain.Entities;

public sealed class MappingRule
{
    public const int MaxSourceCategoryLength = 200;

    private const string InvalidRule = "MappingProfile.InvalidRule";

    private MappingRule(
        Guid id,
        string sourceCategory,
        MappingRuleTarget target,
        Guid? targetCategoryId,
        DateTimeOffset createdAt)
    {
        Id = id;
        SourceCategory = sourceCategory;
        Target = target;
        TargetCategoryId = targetCategoryId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid MappingProfileId { get; private set; }

    public string SourceCategory { get; private set; }

    public MappingRuleTarget Target { get; private set; }

    public Guid? TargetCategoryId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static MappingRule Create(
        Guid id,
        string sourceCategory,
        MappingRuleTarget target,
        Guid? targetCategoryId,
        DateTimeOffset createdAt)
    {
        RequireValidSourceCategory(sourceCategory);
        RequireTargetCategoryMatchesTarget(target, targetCategoryId);

        return new MappingRule(id, sourceCategory, target, targetCategoryId, createdAt);
    }

    internal void BelongTo(Guid mappingProfileId) => MappingProfileId = mappingProfileId;

    // The text is kept exactly as the file writes it: the next file of the same shape writes it
    // the same way, and a trimmed or re-cased copy would stop matching its rows.
    private static void RequireValidSourceCategory(string sourceCategory)
    {
        if (string.IsNullOrWhiteSpace(sourceCategory) || sourceCategory.Length > MaxSourceCategoryLength)
        {
            throw new DomainRuleViolationException(
                InvalidRule,
                "A file category must be between 1 and 200 characters.");
        }
    }

    private static void RequireTargetCategoryMatchesTarget(MappingRuleTarget target, Guid? targetCategoryId)
    {
        if (target == MappingRuleTarget.Category && targetCategoryId is null)
        {
            throw new DomainRuleViolationException(
                InvalidRule,
                "A rule that files a row under a category must name that category.");
        }

        if (target == MappingRuleTarget.TransferBetweenOwnAccounts && targetCategoryId is not null)
        {
            throw new DomainRuleViolationException(
                InvalidRule,
                "A rule for a move between your own accounts records no category.");
        }
    }
}
