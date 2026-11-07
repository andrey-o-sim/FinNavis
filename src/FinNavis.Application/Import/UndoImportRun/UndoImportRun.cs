using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;

namespace FinNavis.Application.Import.UndoImportRun;

public sealed class UndoImportRun
{
    public UndoImportRun(
        IImportRunRepository importRuns,
        ITransactionRepository transactions,
        ITransferRepository transfers,
        IAccountRepository accounts,
        ICategoryRepository categories,
        IImportQueries importQueries,
        IUnitOfWork unitOfWork) =>
        throw new NotImplementedException();

    public Task ExecuteAsync(Guid importRunId, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
