using Microsoft.AspNetCore.Http.HttpResults;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Application.Import.InspectFile;
using FinNavis.Presentation.Errors;

namespace FinNavis.Presentation.Endpoints.MappingProfiles.Inspect;

public static class InspectFileEndpoint
{
    // The API has no sign-in and sets no cookie, so a cross-site form post carries nothing a forged
    // request could borrow. An antiforgery token would protect nothing here.
    public static RouteHandlerBuilder MapInspectFile(this RouteGroupBuilder mappingProfiles) =>
        mappingProfiles
            .MapPost("/inspect", InspectAsync)
            .WithName("inspectFile")
            .DisableAntiforgery()
            .Produces<ErrorEnvelope>(StatusCodes.Status400BadRequest);

    private static async Task<Ok<FileInspectionResponse>> InspectAsync(
        IFormFile file,
        InspectFile useCase,
        CancellationToken cancellationToken)
    {
        await using Stream statement = file.OpenReadStream();

        FileInspection inspection = await useCase.ExecuteAsync(statement, ImportFileKind.Statement, cancellationToken);

        return TypedResults.Ok(FileInspectionResponse.From(inspection));
    }
}
