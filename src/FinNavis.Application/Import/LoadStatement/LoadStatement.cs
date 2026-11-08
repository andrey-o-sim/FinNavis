using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Application.Common;
using FinNavis.Application.Import.Pipeline;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Errors;

namespace FinNavis.Application.Import.LoadStatement;

public sealed class LoadStatement(
    IStatementReader statementReader,
    IAccountRepository accounts,
    IMappingProfileRepository mappingProfiles,
    ImportPipeline pipeline,
    LedgerOptions ledger)
{
    // A statement covers the one account the owner names when handing it over, so only the date and
    // the amount are required of the file itself.
    private static readonly Dictionary<StatementColumn, string> RequiredColumnNames = new()
    {
        [StatementColumn.Date] = "date",
        [StatementColumn.Amount] = "amount",
    };

    public async Task<ImportRun> ExecuteAsync(
        Stream statement,
        string fileName,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        _ = await accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw ImportErrors.AccountNotFound(accountId);

        StatementHeader header = await ReadHeaderAsync(statement, cancellationToken);
        RequireDateAndAmount(header);
        MappingProfile profile = await mappingProfiles.GetByShapeKeyAsync(header.ShapeKey, cancellationToken)
            ?? throw ImportErrors.MappingProfileNotConfirmed(header.ShapeKey);

        StatementPolicy policy = new(accountId, header, profile, ledger.Currency);

        return await RunAsOneLoadAsync(statement, fileName, policy, cancellationToken);
    }

    // The reader throws InvalidDataException when the file turns out not to be a statement, for
    // example when it goes over the row ceiling. That is the owner handing over the wrong file, not a
    // fault, so it is refused like any other unusable file.
    private async Task<StatementHeader> ReadHeaderAsync(Stream statement, CancellationToken cancellationToken)
    {
        try
        {
            return await statementReader.ReadHeaderAsync(statement, ImportFileKind.Statement, cancellationToken);
        }
        catch (InvalidDataException readFailure)
        {
            throw ImportErrors.FileNotReadable(readFailure);
        }
    }

    private static void RequireDateAndAmount(StatementHeader header)
    {
        List<string> missingColumnNames =
        [
            .. RequiredColumnNames
                .Where(required => header.MissingColumns.Contains(required.Key))
                .Select(required => required.Value),
        ];

        if (missingColumnNames.Count > 0)
        {
            throw ImportErrors.MissingRequiredColumn(missingColumnNames);
        }
    }

    // The whole run commits at once, so whatever stopped it, nothing of it reached the ledger. The
    // owner is told exactly that, rather than shown the fault itself. A file that turns out not to be
    // a statement while its rows stream is still the wrong file, not a failed load.
    private async Task<ImportRun> RunAsOneLoadAsync(
        Stream statement,
        string fileName,
        StatementPolicy policy,
        CancellationToken cancellationToken)
    {
        try
        {
            return await pipeline.RunAsync(statement, fileName, policy, cancellationToken);
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
