using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.Import.UndoImportRun;

public sealed class UndoImportRun(
    IImportRunRepository importRuns,
    ITransactionRepository transactions,
    ITransferRepository transfers,
    IAccountRepository accounts,
    ICategoryRepository categories,
    IImportQueries importQueries,
    IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(Guid importRunId, CancellationToken cancellationToken)
    {
        ImportRun run = await importRuns.GetByIdAsync(importRunId, cancellationToken)
            ?? throw ImportErrors.RunNotFound(importRunId);

        run.RequireNotUndone();

        List<Transaction> stillRecordedTransactions =
            await LoadStillRecordedTransactionsAsync(run, cancellationToken);
        List<Transfer> stillRecordedTransfers =
            await LoadStillRecordedTransfersAsync(run, cancellationToken);

        run.RequireUnchangedByHand(stillRecordedTransactions, stillRecordedTransfers);

        ImportRunCreationsInUse creationsInUse =
            await importQueries.GetCreationsNamedOutsideRunAsync(run.Id, cancellationToken);

        run.MarkUndone();

        foreach (Transaction transaction in stillRecordedTransactions)
        {
            transactions.Remove(transaction);
        }

        foreach (Transfer transfer in stillRecordedTransfers)
        {
            transfers.Remove(transfer);
        }

        await RemoveCreatedAccountsNobodyElseNamesAsync(run, creationsInUse, cancellationToken);
        await RemoveCreatedCategoriesNobodyElseNamesAsync(run, creationsInUse, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // A movement the owner deleted by hand is simply absent: there is nothing left to remove, and
    // its absence must not block the undo of the rest.
    private async Task<List<Transaction>> LoadStillRecordedTransactionsAsync(
        ImportRun run,
        CancellationToken cancellationToken)
    {
        List<Transaction> recorded = [];

        foreach (ImportTransactionLink link in run.TransactionLinks)
        {
            Transaction? transaction = await transactions.GetByIdAsync(link.TransactionId, cancellationToken);

            if (transaction is not null)
            {
                recorded.Add(transaction);
            }
        }

        return recorded;
    }

    private async Task<List<Transfer>> LoadStillRecordedTransfersAsync(
        ImportRun run,
        CancellationToken cancellationToken)
    {
        List<Transfer> recorded = [];

        foreach (ImportTransferLink link in run.TransferLinks)
        {
            Transfer? transfer = await transfers.GetByIdAsync(link.TransferId, cancellationToken);

            if (transfer is not null)
            {
                recorded.Add(transfer);
            }
        }

        return recorded;
    }

    private async Task RemoveCreatedAccountsNobodyElseNamesAsync(
        ImportRun run,
        ImportRunCreationsInUse creationsInUse,
        CancellationToken cancellationToken)
    {
        IEnumerable<Guid> unusedAccountIds = run.CreatedAccountLinks
            .Select(link => link.AccountId)
            .Except(creationsInUse.AccountIds);

        foreach (Guid accountId in unusedAccountIds)
        {
            Account? account = await accounts.GetByIdAsync(accountId, cancellationToken);

            if (account is not null)
            {
                accounts.Remove(account);
            }
        }
    }

    private async Task RemoveCreatedCategoriesNobodyElseNamesAsync(
        ImportRun run,
        ImportRunCreationsInUse creationsInUse,
        CancellationToken cancellationToken)
    {
        IEnumerable<Guid> unusedCategoryIds = run.CreatedCategoryLinks
            .Select(link => link.CategoryId)
            .Except(creationsInUse.CategoryIds);

        foreach (Guid categoryId in unusedCategoryIds)
        {
            Category? category = await categories.GetByIdAsync(categoryId, cancellationToken);

            if (category is not null)
            {
                categories.Remove(category);
            }
        }
    }
}
