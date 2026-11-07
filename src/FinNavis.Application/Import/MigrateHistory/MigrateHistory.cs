using FinNavis.Application.Abstractions;
using FinNavis.Application.Import.Pipeline;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.Import.MigrateHistory;

public sealed class MigrateHistory
{
    public MigrateHistory(
        IStatementReader statementReader,
        IAccountRepository accounts,
        ICategoryRepository categories,
        ImportPipeline pipeline,
        IClock clock) =>
        throw new NotImplementedException();

    public Task<ImportRun> ExecuteAsync(
        Stream export,
        string fileName,
        CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
