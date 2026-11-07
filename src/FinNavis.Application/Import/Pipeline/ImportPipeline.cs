using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.Import.Pipeline;

public sealed class ImportPipeline
{
    public ImportPipeline(
        IStatementReader reader,
        IImportQueries importQueries,
        ITransactionRepository transactions,
        IImportRunRepository importRuns,
        IUnitOfWork unitOfWork,
        IClock clock) =>
        throw new NotImplementedException();

    public Task<ImportRun> RunAsync(
        Stream file,
        string fileName,
        IImportPolicy policy,
        CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
