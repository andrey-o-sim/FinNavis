using Microsoft.AspNetCore.Mvc;

namespace FinNavis.Presentation.Endpoints.ImportRuns.LoadStatement;

public sealed record LoadStatementRequest(
    [FromForm(Name = "file")] IFormFile File,
    [FromForm(Name = "account_id")] Guid AccountId);
