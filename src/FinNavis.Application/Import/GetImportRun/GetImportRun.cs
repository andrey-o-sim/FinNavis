using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.Import.GetImportRun;

public sealed class GetImportRun(IImportRunRepository importRuns)
{
    public async Task<ImportRunReport> ExecuteAsync(Guid importRunId, CancellationToken cancellationToken)
    {
        ImportRun run = await importRuns.GetByIdAsync(importRunId, cancellationToken)
            ?? throw ImportErrors.RunNotFound(importRunId);

        return new ImportRunReport(
            run.Id,
            run.Kind,
            run.Status,
            run.FileName,
            run.AccountId,
            run.RecordedRowCount,
            run.AlreadyPresentRowCount,
            run.CreatedAt,
            ReportSetAsideRowsInFileOrder(run));
    }

    private static List<ReportedSetAsideRow> ReportSetAsideRowsInFileOrder(ImportRun run) =>
    [
        .. run.SetAsideRows
            .OrderBy(row => row.RowNumber)
            .Select(row => new ReportedSetAsideRow(row.RowNumber, row.Reason, row.SourceCategory, row.RawLine)),
    ];
}
