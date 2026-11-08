using Microsoft.AspNetCore.Http.HttpResults;
using FinNavis.Domain.Entities;
using FinNavis.Presentation.Errors;
using MigrateHistoryUseCase = FinNavis.Application.Import.MigrateHistory.MigrateHistory;

namespace FinNavis.Presentation.Endpoints.ImportRuns.MigrateHistory;

public static class MigrateHistoryEndpoint
{
    // A move that finished is a 200 even when it set rows aside: those rows are a normal outcome and
    // travel in the body. The run id is in the body too, so there is no Location header and no 201.
    // The API has no sign-in and sets no cookie, so an antiforgery token would protect nothing here.
    public static RouteHandlerBuilder MapMigrateHistory(this RouteGroupBuilder importRuns) =>
        importRuns
            .MapPost("/history-migration", MigrateAsync)
            .WithName("migrateHistory")
            .DisableAntiforgery()
            .Produces<ErrorEnvelope>(StatusCodes.Status400BadRequest)
            .Produces<ErrorEnvelope>(StatusCodes.Status500InternalServerError);

    private static async Task<Ok<MigrateHistoryResponse>> MigrateAsync(
        [AsParameters] MigrateHistoryRequest request,
        MigrateHistoryUseCase useCase,
        CancellationToken cancellationToken)
    {
        await using Stream export = request.File.OpenReadStream();

        ImportRun run = await useCase.ExecuteAsync(export, request.File.FileName, cancellationToken);

        return TypedResults.Ok(MigrateHistoryResponse.From(run));
    }
}
