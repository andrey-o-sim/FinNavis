using Microsoft.AspNetCore.Mvc;

namespace FinNavis.Presentation.Endpoints.ImportRuns.Get;

public sealed record GetImportRunRequest(
    [FromRoute(Name = "import_run_id")] Guid ImportRunId,
    [FromQuery(Name = "after")] string? After,
    [FromQuery(Name = "before")] string? Before,
    [FromQuery(Name = "limit")] int? Limit);
