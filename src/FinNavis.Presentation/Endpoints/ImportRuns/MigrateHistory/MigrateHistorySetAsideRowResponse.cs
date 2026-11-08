using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;

namespace FinNavis.Presentation.Endpoints.ImportRuns.MigrateHistory;

public sealed record MigrateHistorySetAsideRowResponse(
    int RowNumber,
    SetAsideReason Reason,
    string? SourceCategory,
    string RawLine)
{
    public static MigrateHistorySetAsideRowResponse From(SetAsideRow row) =>
        new(row.RowNumber, row.Reason, row.SourceCategory, row.RawLine);
}
