using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;

namespace FinNavis.Presentation.Endpoints.ImportRuns.LoadStatement;

public sealed record LoadStatementResponse(
    Guid ImportRunId,
    ImportRunKind Kind,
    ImportRunStatus Status,
    string FileName,
    Guid? AccountId,
    int RecordedRowCount,
    int AlreadyPresentRowCount,
    DateTimeOffset CreatedAt,
    IReadOnlyList<LoadStatementSetAsideRowResponse> SetAside)
{
    public static LoadStatementResponse From(ImportRun run) =>
        new(
            run.Id,
            run.Kind,
            run.Status,
            run.FileName,
            run.AccountId,
            run.RecordedRowCount,
            run.AlreadyPresentRowCount,
            run.CreatedAt,
            [.. run.SetAsideRows.OrderBy(row => row.RowNumber).Select(LoadStatementSetAsideRowResponse.From)]);
}
