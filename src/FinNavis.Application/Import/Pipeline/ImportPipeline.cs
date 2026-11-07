using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Import.Pipeline;

public sealed class ImportPipeline(
    IStatementReader reader,
    IImportQueries importQueries,
    ITransactionRepository transactions,
    IImportRunRepository importRuns,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ImportRun> RunAsync(
        Stream file,
        string fileName,
        IImportPolicy policy,
        CancellationToken cancellationToken)
    {
        List<(StatementRow Row, RowDecision Decision)> decidedRows = [];

        await foreach (StatementRow row in reader.ReadRowsAsync(file, policy.FileKind, cancellationToken))
        {
            decidedRows.Add((row, policy.Decide(row)));
        }

        List<RecordAsTransaction> toRecord =
            [.. decidedRows.Select(decided => decided.Decision).OfType<RecordAsTransaction>()];
        DuplicateMatcher matcher =
            await MatchAgainstSnapshotAsync(toRecord, policy.MatchWindowDays, cancellationToken);

        ImportRun run = ImportRun.Record(
            Guid.CreateVersion7(), fileName, policy.RunKind, policy.RunAccountId, clock.UtcNow);

        foreach ((StatementRow row, RowDecision decision) in decidedRows)
        {
            switch (decision)
            {
                case RecordAsTransaction record when matcher.TryClaim(record):
                    run.CountAlreadyPresentRow();
                    break;
                case RecordAsTransaction record:
                    RecordTransaction(run, record);
                    break;
                case SetRowAside setAside:
                    run.SetAside(
                        Guid.CreateVersion7(), row.LineNumber, setAside.Reason, setAside.SourceCategory, row.RawLine);
                    break;
            }
        }

        importRuns.Add(run);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return run;
    }

    // Taken once, before anything is written, so the run can never match a row against a movement
    // it recorded itself.
    private async Task<DuplicateMatcher> MatchAgainstSnapshotAsync(
        IReadOnlyList<RecordAsTransaction> toRecord,
        int matchWindowDays,
        CancellationToken cancellationToken)
    {
        if (toRecord.Count == 0)
        {
            return new DuplicateMatcher([], matchWindowDays);
        }

        List<Guid> accountIds = [.. toRecord.Select(record => record.AccountId).Distinct()];
        DateRange period = DateRange.Create(
            toRecord.Min(record => record.OccurredOn).AddDays(-matchWindowDays),
            toRecord.Max(record => record.OccurredOn).AddDays(matchWindowDays));

        CandidateSnapshot snapshot =
            await importQueries.GetCandidateSnapshotAsync(accountIds, period, cancellationToken);

        return new DuplicateMatcher(snapshot.Transactions, matchWindowDays);
    }

    private void RecordTransaction(ImportRun run, RecordAsTransaction record)
    {
        Transaction transaction = Transaction.Record(
            Guid.CreateVersion7(),
            record.AccountId,
            record.CategoryId,
            record.Amount,
            record.OccurredOn,
            record.Note,
            run.CreatedAt);

        transactions.Add(transaction);
        run.LinkRecordedTransaction(Guid.CreateVersion7(), transaction);
    }
}
