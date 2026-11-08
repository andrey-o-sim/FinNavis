using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;

namespace FinNavis.Presentation.Endpoints.ImportRuns.LoadStatement;

public sealed record LoadStatementSetAsideRowResponse(
    int RowNumber,
    SetAsideReason Reason,
    string? SourceCategory,
    string RawLine)
{
    public static LoadStatementSetAsideRowResponse From(SetAsideRow row) =>
        new(row.RowNumber, row.Reason, row.SourceCategory, row.RawLine);
}
