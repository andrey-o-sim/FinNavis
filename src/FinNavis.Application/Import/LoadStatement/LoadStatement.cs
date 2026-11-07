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
    public async Task<ImportRun> ExecuteAsync(
        Stream statement,
        string fileName,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        _ = await accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw ImportErrors.AccountNotFound(accountId);

        StatementHeader header =
            await statementReader.ReadHeaderAsync(statement, ImportFileKind.Statement, cancellationToken);
        MappingProfile profile = await mappingProfiles.GetByShapeKeyAsync(header.ShapeKey, cancellationToken)
            ?? throw ImportErrors.MappingProfileNotConfirmed(header.ShapeKey);

        StatementPolicy policy = new(accountId, header, profile, ledger.Currency);

        return await RunAsOneLoadAsync(statement, fileName, policy, cancellationToken);
    }

    // The whole run commits at once, so whatever stopped it, nothing of it reached the ledger. The
    // owner is told exactly that, rather than shown the fault itself.
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
        catch (Exception fault) when (fault is not DomainRuleViolationException and not OperationCanceledException)
        {
            throw ImportErrors.NotRecorded(fault);
        }
    }
}
