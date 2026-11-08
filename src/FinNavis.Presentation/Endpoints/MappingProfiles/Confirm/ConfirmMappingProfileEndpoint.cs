using Microsoft.AspNetCore.Http.HttpResults;
using FinNavis.Application.Import.ConfirmMappingProfile;
using FinNavis.Presentation.Errors;

namespace FinNavis.Presentation.Endpoints.MappingProfiles.Confirm;

public static class ConfirmMappingProfileEndpoint
{
    public static RouteHandlerBuilder MapConfirmMappingProfile(this RouteGroupBuilder mappingProfiles) =>
        mappingProfiles
            .MapPost("/", ConfirmAsync)
            .WithName("confirmMappingProfile")
            .Produces<ErrorEnvelope>(StatusCodes.Status400BadRequest)
            .Produces<ErrorEnvelope>(StatusCodes.Status404NotFound)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);

    private static async Task<Results<Created<MappingProfileResponse>, Ok<MappingProfileResponse>>> ConfirmAsync(
        ConfirmMappingProfileRequest request,
        ConfirmMappingProfile useCase,
        CancellationToken cancellationToken)
    {
        List<RuleToConfirm> rules =
        [
            .. request.Rules.Select(rule => new RuleToConfirm(rule.SourceCategory, rule.Target, rule.TargetCategoryId)),
        ];

        MappingProfileConfirmation confirmation = await useCase.ExecuteAsync(request.ShapeKey, rules, cancellationToken);
        MappingProfileResponse response = MappingProfileResponse.From(confirmation.Profile);

        return confirmation.IsNewShape
            ? TypedResults.Created($"/api/v1/mapping-profiles/{response.Id}", response)
            : TypedResults.Ok(response);
    }
}
