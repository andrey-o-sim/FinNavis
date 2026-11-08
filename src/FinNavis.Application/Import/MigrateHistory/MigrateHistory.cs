using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Application.Import.Pipeline;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Errors;

namespace FinNavis.Application.Import.MigrateHistory;

public sealed class MigrateHistory(
    IStatementReader statementReader,
    IAccountRepository accounts,
    ICategoryRepository categories,
    ImportPipeline pipeline,
    IClock clock)
{
    // An export covers every account the owner kept, so unlike a statement it must also name the
    // account of each row.
    private static readonly Dictionary<StatementColumn, string> RequiredColumnNames = new()
    {
        [StatementColumn.Date] = "date",
        [StatementColumn.Amount] = "amount",
        [StatementColumn.Account] = "account",
    };

    public async Task<ImportRun> ExecuteAsync(
        Stream export,
        string fileName,
        CancellationToken cancellationToken)
    {
        StatementHeader header = await ReadHeaderAsync(export, cancellationToken);
        RequireDateAmountAndAccount(header);

        LedgerByName ledger = new(
            await accounts.ListAsync(cancellationToken),
            await categories.ListAsync(cancellationToken),
            accounts,
            categories,
            clock.UtcNow);
        MigrationPolicy policy = new(header, ledger);

        return await RunAsOneLoadAsync(export, fileName, policy, cancellationToken);
    }

    // The reader throws InvalidDataException when the file turns out not to be an export, for
    // example when it goes over the row ceiling. That is the owner handing over the wrong file, not a
    // fault, so it is refused like any other unusable file.
    private async Task<StatementHeader> ReadHeaderAsync(Stream export, CancellationToken cancellationToken)
    {
        try
        {
            return await statementReader.ReadHeaderAsync(export, ImportFileKind.Export, cancellationToken);
        }
        catch (InvalidDataException readFailure)
        {
            throw ImportErrors.FileNotReadable(readFailure);
        }
    }

    private static void RequireDateAmountAndAccount(StatementHeader header)
    {
        List<string> missingColumnNames =
        [
            .. RequiredColumnNames
                .Where(required => header.MissingColumns.Contains(required.Key))
                .Select(required => required.Value),
        ];

        if (missingColumnNames.Count > 0)
        {
            throw ImportErrors.MissingRequiredExportColumn(missingColumnNames);
        }
    }

    // The whole run commits at once, so whatever stopped it, nothing of it reached the ledger — not
    // the movements and not the accounts and categories it would have created. The owner is told
    // exactly that, rather than shown the fault itself. A file that turns out not to be an export
    // while its rows stream is still the wrong file, not a failed load.
    private async Task<ImportRun> RunAsOneLoadAsync(
        Stream export,
        string fileName,
        MigrationPolicy policy,
        CancellationToken cancellationToken)
    {
        try
        {
            return await pipeline.RunAsync(export, fileName, policy, cancellationToken);
        }
        catch (InvalidDataException readFailure)
        {
            throw ImportErrors.FileNotReadable(readFailure);
        }
        catch (Exception fault) when (fault is not DomainRuleViolationException and not OperationCanceledException)
        {
            throw ImportErrors.NotRecorded(fault);
        }
    }
}
