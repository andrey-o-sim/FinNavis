using FinNavis.Application.Abstractions;
using FinNavis.Application.Common;
using FinNavis.Application.Import.Pipeline;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.Import.LoadStatement;

public sealed class LoadStatement
{
    public LoadStatement(
        IStatementReader statementReader,
        IAccountRepository accounts,
        IMappingProfileRepository mappingProfiles,
        ImportPipeline pipeline,
        LedgerOptions ledger) =>
        throw new NotImplementedException();

    public Task<ImportRun> ExecuteAsync(
        Stream statement,
        string fileName,
        Guid accountId,
        CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
