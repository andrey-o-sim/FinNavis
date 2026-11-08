using FinNavis.Domain.Enums;

namespace FinNavis.Presentation.Endpoints.MappingProfiles.Confirm;

public sealed record MappingRuleRequest(string SourceCategory, MappingRuleTarget Target, Guid? TargetCategoryId);
