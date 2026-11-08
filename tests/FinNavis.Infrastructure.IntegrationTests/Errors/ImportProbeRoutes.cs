using FinNavis.Application.Abstractions.Reads;
using FinNavis.Application.Import.ConfirmMappingProfile;
using FinNavis.Application.Import.GetImportRun;
using FinNavis.Application.Import.InspectFile;
using FinNavis.Application.Import.LoadStatement;
using FinNavis.Application.Import.UndoImportRun;
using FinNavis.Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FinNavis.Infrastructure.IntegrationTests.Errors;

// The csv-import endpoints do not exist yet, so these probes call the real use cases the way an
// endpoint would. A refusal then travels from the code that raises it through the API's pipeline.
internal static class ImportProbeRoutes
{
    public const string ImportRunRoute = "/probes/import-runs/{0}";

    public const string UndoRoute = "/probes/import-runs/{0}/undo";

    public const string InspectRoute = "/probes/mapping-profiles/inspect";

    public const string ConfirmRoute = "/probes/mapping-profiles/{0}";

    public const string LoadRoute = "/probes/import-runs/statement/{0}";

    public const string SourceCategory = "Groceries";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/probes/import-runs/{importRunId:guid}",
            async (Guid importRunId, int? limit, GetImportRun useCase, CancellationToken cancellationToken) =>
                Results.Ok(new { limit, report = await useCase.ExecuteAsync(importRunId, cancellationToken) }));

        endpoints.MapPost(
            "/probes/import-runs/{importRunId:guid}/undo",
            async (Guid importRunId, UndoImportRun useCase, CancellationToken cancellationToken) =>
            {
                await useCase.ExecuteAsync(importRunId, cancellationToken);
                return Results.NoContent();
            });

        endpoints.MapPost(
            InspectRoute,
            async (HttpRequest request, InspectFile useCase, CancellationToken cancellationToken) =>
            {
                using MemoryStream file = await ReadBodyAsync(request, cancellationToken);
                return Results.Ok(await useCase.ExecuteAsync(file, ImportFileKind.Statement, cancellationToken));
            });

        endpoints.MapPost(
            "/probes/mapping-profiles/{targetCategoryId:guid}",
            async (Guid targetCategoryId, ConfirmMappingProfile useCase, CancellationToken cancellationToken) =>
            {
                RuleToConfirm rule = new(SourceCategory, MappingRuleTarget.Category, targetCategoryId);
                await useCase.ExecuteAsync("date|category|amount", [rule], cancellationToken);
                return Results.NoContent();
            });

        endpoints.MapPost(
            "/probes/import-runs/statement/{accountId:guid}",
            async (Guid accountId, HttpRequest request, LoadStatement useCase, CancellationToken cancellationToken) =>
            {
                using MemoryStream file = await ReadBodyAsync(request, cancellationToken);
                await useCase.ExecuteAsync(file, "statement.csv", accountId, cancellationToken);
                return Results.NoContent();
            });
    }

    // The reader goes over the file more than once, so it gets a stream it can rewind.
    private static async Task<MemoryStream> ReadBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        MemoryStream file = new();
        await request.Body.CopyToAsync(file, cancellationToken);
        file.Position = 0;

        return file;
    }
}
