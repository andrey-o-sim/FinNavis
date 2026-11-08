using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using FinNavis.Application.Import.GetImportRun;
using FinNavis.Presentation.Errors;
using GetImportRunUseCase = FinNavis.Application.Import.GetImportRun.GetImportRun;
using UndoImportRunUseCase = FinNavis.Application.Import.UndoImportRun.UndoImportRun;

namespace FinNavis.Presentation.Endpoints.ImportRuns.Undo;

public static class UndoImportRunEndpoint
{
    // The run id in the path is the whole request: no body and no Idempotency-Key, because a second
    // undo of the same run is refused as AlreadyUndone rather than repeated. The API has no sign-in
    // and sets no cookie, so an antiforgery token would protect nothing here.
    public static RouteHandlerBuilder MapUndoImportRun(this RouteGroupBuilder importRuns) =>
        importRuns
            .MapPost("/{import_run_id:guid}/undo", UndoAsync)
            .WithName("undoImportRun")
            .DisableAntiforgery()
            .Produces<ErrorEnvelope>(StatusCodes.Status404NotFound)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);

    // The undo takes no paging parameters, so its report carries the first page of the set-aside
    // rows. The rest is read through the run's GET with the cursor this page hands back.
    private static async Task<Ok<UndoImportRunResponse>> UndoAsync(
        [FromRoute(Name = "import_run_id")] Guid importRunId,
        UndoImportRunUseCase undo,
        GetImportRunUseCase read,
        CancellationToken cancellationToken)
    {
        await undo.ExecuteAsync(importRunId, cancellationToken);

        ImportRunReport report =
            await read.ExecuteAsync(importRunId, SetAsidePageRequest.FirstPage, cancellationToken);

        return TypedResults.Ok(UndoImportRunResponse.From(report));
    }
}
