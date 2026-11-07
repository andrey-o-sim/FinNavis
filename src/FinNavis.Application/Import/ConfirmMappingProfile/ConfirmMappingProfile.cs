using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.Import.ConfirmMappingProfile;

public sealed class ConfirmMappingProfile(
    ICategoryRepository categories,
    IMappingProfileRepository mappingProfiles,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<MappingProfileConfirmation> ExecuteAsync(
        string shapeKey,
        IReadOnlyList<RuleToConfirm> rules,
        CancellationToken cancellationToken)
    {
        await RequireEveryTargetCategoryAsync(rules, cancellationToken);

        DateTimeOffset now = clock.UtcNow;
        List<MappingRule> mappingRules = BuildMappingRules(rules, now);

        MappingProfile? existing = await mappingProfiles.GetByShapeKeyAsync(shapeKey, cancellationToken);
        MappingProfileConfirmation confirmation = existing is null
            ? CreateProfile(shapeKey, mappingRules, now)
            : ReplaceRulesOf(existing, mappingRules);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return confirmation;
    }

    private static List<MappingRule> BuildMappingRules(IReadOnlyList<RuleToConfirm> rules, DateTimeOffset now) =>
    [
        .. rules.Select(rule => MappingRule.Create(
            Guid.CreateVersion7(),
            rule.SourceCategory,
            rule.Target,
            rule.TargetCategoryId,
            now)),
    ];

    private MappingProfileConfirmation CreateProfile(
        string shapeKey,
        IReadOnlyCollection<MappingRule> mappingRules,
        DateTimeOffset now)
    {
        MappingProfile profile = MappingProfile.Create(Guid.CreateVersion7(), shapeKey, mappingRules, now);
        mappingProfiles.Add(profile);

        return new MappingProfileConfirmation(profile, IsNewShape: true);
    }

    private MappingProfileConfirmation ReplaceRulesOf(MappingProfile existing, IReadOnlyCollection<MappingRule> mappingRules)
    {
        mappingProfiles.ReplaceRules(existing, mappingRules);

        return new MappingProfileConfirmation(existing, IsNewShape: false);
    }

    private async Task RequireEveryTargetCategoryAsync(
        IReadOnlyList<RuleToConfirm> rules,
        CancellationToken cancellationToken)
    {
        foreach (RuleToConfirm rule in rules)
        {
            if (rule.TargetCategoryId is not { } targetCategoryId)
            {
                continue;
            }

            _ = await categories.GetByIdAsync(targetCategoryId, cancellationToken)
                ?? throw ImportErrors.CategoryNotFound(rule.SourceCategory, targetCategoryId);
        }
    }
}
