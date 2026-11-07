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
        List<MappingRule> mappingRules =
        [
            .. rules.Select(rule => MappingRule.Create(
                Guid.CreateVersion7(),
                rule.SourceCategory,
                rule.Target,
                rule.TargetCategoryId,
                now)),
        ];

        MappingProfile? existing = await mappingProfiles.GetByShapeKeyAsync(shapeKey, cancellationToken);
        MappingProfileConfirmation confirmation;

        if (existing is null)
        {
            MappingProfile profile = MappingProfile.Create(Guid.CreateVersion7(), shapeKey, mappingRules, now);
            mappingProfiles.Add(profile);
            confirmation = new MappingProfileConfirmation(profile, IsNewShape: true);
        }
        else
        {
            mappingProfiles.ReplaceRules(existing, mappingRules);
            confirmation = new MappingProfileConfirmation(existing, IsNewShape: false);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return confirmation;
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
