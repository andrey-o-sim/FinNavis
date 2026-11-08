using FinNavis.Domain.Entities;

namespace FinNavis.Presentation.Endpoints.MappingProfiles.Confirm;

public sealed record MappingProfileResponse(
    Guid Id,
    string ShapeKey,
    IReadOnlyList<MappingRuleResponse> Rules,
    DateTimeOffset CreatedAt)
{
    public static MappingProfileResponse From(MappingProfile profile) =>
        new(profile.Id, profile.ShapeKey, [.. profile.Rules.Select(MappingRuleResponse.From)], profile.CreatedAt);
}
