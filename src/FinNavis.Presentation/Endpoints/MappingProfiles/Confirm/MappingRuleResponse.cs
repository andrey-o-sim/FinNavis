using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;

namespace FinNavis.Presentation.Endpoints.MappingProfiles.Confirm;

public sealed record MappingRuleResponse(string SourceCategory, MappingRuleTarget Target, Guid? TargetCategoryId)
{
    public static MappingRuleResponse From(MappingRule rule) =>
        new(rule.SourceCategory, rule.Target, rule.TargetCategoryId);
}
