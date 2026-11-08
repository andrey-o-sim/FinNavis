using Microsoft.AspNetCore.Mvc;

namespace FinNavis.Presentation.Endpoints.ImportRuns.MigrateHistory;

public sealed record MigrateHistoryRequest([FromForm(Name = "file")] IFormFile File);
