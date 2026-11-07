using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Application.Import.Pipeline;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

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
        StatementHeader header =
            await statementReader.ReadHeaderAsync(export, ImportFileKind.Export, cancellationToken);
        RequireDateAmountAndAccount(header);

        LedgerByName ledger = new(
            await accounts.ListAsync(cancellationToken),
            await categories.ListAsync(cancellationToken),
            accounts,
            categories,
            clock.UtcNow);
        MigrationPolicy policy = new(header, ledger);

        return await pipeline.RunAsync(export, fileName, policy, cancellationToken);
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
}
