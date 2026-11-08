using FinNavis.Application.Import.GetImportRun;
using FinNavis.Domain.Enums;

namespace FinNavis.Presentation.Endpoints.ImportRuns.Get;

public sealed record GetImportRunSetAsideRowResponse(
    int RowNumber,
    SetAsideReason Reason,
    string? SourceCategory,
    string RawLine)
{
    public static GetImportRunSetAsideRowResponse From(ReportedSetAsideRow row) =>
        new(row.RowNumber, row.Reason, row.SourceCategory, row.RawLine);
}
