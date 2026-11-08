using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.Import.GetImportRun;

public sealed class GetImportRun(IImportRunRepository importRuns)
{
    public Task<ImportRunReport> ExecuteAsync(Guid importRunId, CancellationToken cancellationToken) =>
        ExecuteAsync(importRunId, SetAsidePageRequest.FirstPage, cancellationToken);

    public async Task<ImportRunReport> ExecuteAsync(
        Guid importRunId,
        SetAsidePageRequest page,
        CancellationToken cancellationToken)
    {
        RequireLimitInRange(page.Limit);

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
            ReadSetAsidePage(run, page));
    }

    private static void RequireLimitInRange(int limit)
    {
        if (limit is < SetAsidePageRequest.MinLimit or > SetAsidePageRequest.MaxLimit)
        {
            throw ImportErrors.PagingLimitOutOfRange(limit, SetAsidePageRequest.MinLimit, SetAsidePageRequest.MaxLimit);
        }
    }

    // A run's set-aside rows are written once by the load and never change afterwards, and a run
    // holds at most one file's worth of them, so the page is cut from the rows the run already
    // carries.
    private static SetAsideRowPage ReadSetAsidePage(ImportRun run, SetAsidePageRequest page)
    {
        List<ReportedSetAsideRow> rowsInFileOrder = ReportSetAsideRowsInFileOrder(run);
        (int start, int end) = PageBounds(run.Id, rowsInFileOrder, page);

        List<ReportedSetAsideRow> items = rowsInFileOrder[start..end];
        bool hasPrev = items.Count > 0 && start > 0;
        bool hasNext = items.Count > 0 && end < rowsInFileOrder.Count;

        return new SetAsideRowPage(
            items,
            hasNext,
            hasPrev,
            hasNext ? new SetAsideCursor(run.Id, items[^1].RowNumber).Encode() : null,
            hasPrev ? new SetAsideCursor(run.Id, items[0].RowNumber).Encode() : null);
    }

    private static (int Start, int End) PageBounds(
        Guid importRunId,
        List<ReportedSetAsideRow> rowsInFileOrder,
        SetAsidePageRequest page)
    {
        if (page.After is not null && page.Before is not null)
        {
            throw ImportErrors.PagingCursorNotMinted();
        }

        if (page.After is not null)
        {
            int start = PositionOf(page.After, importRunId, rowsInFileOrder) + 1;
            return (start, Math.Min(start + page.Limit, rowsInFileOrder.Count));
        }

        if (page.Before is not null)
        {
            int end = PositionOf(page.Before, importRunId, rowsInFileOrder);
            return (Math.Max(end - page.Limit, 0), end);
        }

        return (0, Math.Min(page.Limit, rowsInFileOrder.Count));
    }

    private static int PositionOf(string token, Guid importRunId, List<ReportedSetAsideRow> rowsInFileOrder)
    {
        if (!SetAsideCursor.TryDecode(token, out SetAsideCursor cursor) || cursor.ImportRunId != importRunId)
        {
            throw ImportErrors.PagingCursorNotMinted();
        }

        int position = rowsInFileOrder.FindIndex(row => row.RowNumber == cursor.RowNumber);

        return position >= 0 ? position : throw ImportErrors.PagingCursorNotMinted();
    }

    private static List<ReportedSetAsideRow> ReportSetAsideRowsInFileOrder(ImportRun run) =>
    [
        .. run.SetAsideRows
            .OrderBy(row => row.RowNumber)
            .Select(row => new ReportedSetAsideRow(row.RowNumber, row.Reason, row.SourceCategory, row.RawLine)),
    ];
}
