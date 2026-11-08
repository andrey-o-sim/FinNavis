using Microsoft.AspNetCore.Http.HttpResults;
using FinNavis.Domain.Entities;
using FinNavis.Presentation.Errors;
using LoadStatementUseCase = FinNavis.Application.Import.LoadStatement.LoadStatement;

namespace FinNavis.Presentation.Endpoints.ImportRuns.LoadStatement;

public static class LoadStatementEndpoint
{
    // A load that finished is a 200 even when it set rows aside: those rows are a normal outcome and
    // travel in the body. The run id is in the body too, so there is no Location header and no 201.
    // The API has no sign-in and sets no cookie, so an antiforgery token would protect nothing here.
    public static RouteHandlerBuilder MapLoadStatement(this RouteGroupBuilder importRuns) =>
        importRuns
            .MapPost("/statement", LoadAsync)
            .WithName("loadStatement")
            .DisableAntiforgery()
            .Produces<ErrorEnvelope>(StatusCodes.Status400BadRequest)
            .Produces<ErrorEnvelope>(StatusCodes.Status404NotFound)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict)
            .Produces<ErrorEnvelope>(StatusCodes.Status500InternalServerError);

    private static async Task<Ok<LoadStatementResponse>> LoadAsync(
        [AsParameters] LoadStatementRequest request,
        LoadStatementUseCase useCase,
        CancellationToken cancellationToken)
    {
        await using Stream statement = request.File.OpenReadStream();

        ImportRun run = await useCase.ExecuteAsync(
            statement, request.File.FileName, request.AccountId, cancellationToken);

        return TypedResults.Ok(LoadStatementResponse.From(run));
    }
}
