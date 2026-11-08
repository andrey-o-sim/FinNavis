using FinNavis.Application.Import.GetImportRun;
using FinNavis.Domain.Enums;

namespace FinNavis.Presentation.Endpoints.ImportRuns.Undo;

public sealed record UndoImportRunSetAsideRowResponse(
    int RowNumber,
    SetAsideReason Reason,
    string? SourceCategory,
    string RawLine)
{
    public static UndoImportRunSetAsideRowResponse From(ReportedSetAsideRow row) =>
        new(row.RowNumber, row.Reason, row.SourceCategory, row.RawLine);
}
