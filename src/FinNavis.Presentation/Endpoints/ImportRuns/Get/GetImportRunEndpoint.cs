using Microsoft.AspNetCore.Http.HttpResults;
using FinNavis.Application.Import.GetImportRun;
using FinNavis.Presentation.Errors;
using GetImportRunUseCase = FinNavis.Application.Import.GetImportRun.GetImportRun;

namespace FinNavis.Presentation.Endpoints.ImportRuns.Get;

public static class GetImportRunEndpoint
{
    public static RouteHandlerBuilder MapGetImportRun(this RouteGroupBuilder importRuns) =>
        importRuns
            .MapGet("/{import_run_id:guid}", GetAsync)
            .WithName("getImportRun")
            .Produces<ErrorEnvelope>(StatusCodes.Status400BadRequest)
            .Produces<ErrorEnvelope>(StatusCodes.Status404NotFound);

    private static async Task<Ok<GetImportRunResponse>> GetAsync(
        [AsParameters] GetImportRunRequest request,
        GetImportRunUseCase useCase,
        CancellationToken cancellationToken)
    {
        SetAsidePageRequest page = new(
            request.After,
            request.Before,
            request.Limit ?? SetAsidePageRequest.DefaultLimit);

        ImportRunReport report = await useCase.ExecuteAsync(request.ImportRunId, page, cancellationToken);

        return TypedResults.Ok(GetImportRunResponse.From(report));
    }
}
