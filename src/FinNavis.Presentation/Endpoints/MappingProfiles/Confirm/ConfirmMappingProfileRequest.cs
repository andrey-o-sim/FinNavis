namespace FinNavis.Presentation.Endpoints.MappingProfiles.Confirm;

public sealed record ConfirmMappingProfileRequest(string ShapeKey, IReadOnlyList<MappingRuleRequest> Rules);
