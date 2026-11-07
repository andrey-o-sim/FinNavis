using FinNavis.Domain.Enums;

namespace FinNavis.Application.Import.ConfirmMappingProfile;

public sealed record RuleToConfirm(string SourceCategory, MappingRuleTarget Target, Guid? TargetCategoryId);
