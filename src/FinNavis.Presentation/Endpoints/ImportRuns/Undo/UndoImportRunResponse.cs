using FinNavis.Application.Import.GetImportRun;
using FinNavis.Domain.Enums;

namespace FinNavis.Presentation.Endpoints.ImportRuns.Undo;

public sealed record UndoImportRunResponse(
    Guid ImportRunId,
    ImportRunKind Kind,
    ImportRunStatus Status,
    string FileName,
    Guid? AccountId,
    int RecordedRowCount,
    int AlreadyPresentRowCount,
    DateTimeOffset CreatedAt,
    UndoImportRunSetAsidePageResponse SetAside)
{
    public static UndoImportRunResponse From(ImportRunReport report) =>
        new(
            report.ImportRunId,
            report.Kind,
            report.Status,
            report.FileName,
            report.AccountId,
            report.RecordedRowCount,
            report.AlreadyPresentRowCount,
            report.CreatedAt,
            UndoImportRunSetAsidePageResponse.From(report.SetAside));
}
