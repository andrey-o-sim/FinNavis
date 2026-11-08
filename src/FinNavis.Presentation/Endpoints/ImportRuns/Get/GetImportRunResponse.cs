using FinNavis.Application.Import.GetImportRun;
using FinNavis.Domain.Enums;

namespace FinNavis.Presentation.Endpoints.ImportRuns.Get;

public sealed record GetImportRunResponse(
    Guid ImportRunId,
    ImportRunKind Kind,
    ImportRunStatus Status,
    string FileName,
    Guid? AccountId,
    int RecordedRowCount,
    int AlreadyPresentRowCount,
    DateTimeOffset CreatedAt,
    GetImportRunSetAsidePageResponse SetAside)
{
    public static GetImportRunResponse From(ImportRunReport report) =>
        new(
            report.ImportRunId,
            report.Kind,
            report.Status,
            report.FileName,
            report.AccountId,
            report.RecordedRowCount,
            report.AlreadyPresentRowCount,
            report.CreatedAt,
            GetImportRunSetAsidePageResponse.From(report.SetAside));
}
