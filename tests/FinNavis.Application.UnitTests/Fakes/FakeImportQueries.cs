using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.Entities;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.UnitTests.Fakes;

// Answers from what the fake repositories hold after their last commit, so a movement the use case
// only staged for removal still counts until the unit of work commits.
internal sealed class FakeImportQueries(
    FakeImportRunRepository runs,
    FakeTransactionRepository transactions,
    FakeTransferRepository transfers,
    FakeCategoryRepository categories) : IImportQueries
{
    public List<Guid> CreationsRequestedForRunIds { get; } = [];

    public List<CancellationToken> TokensSeen { get; } = [];

    public int SnapshotsTaken { get; private set; }

    public Task<CandidateSnapshot> GetCandidateSnapshotAsync(
        IReadOnlyCollection<Guid> accountIds,
        DateRange period,
        CancellationToken cancellationToken)
    {
        TokensSeen.Add(cancellationToken);
        SnapshotsTaken++;

        List<CandidateTransactionRow> transactionRows =
        [
            .. transactions.Stored
                .Where(transaction => accountIds.Contains(transaction.AccountId)
                    && IsWithin(period, transaction.OccurredOn))
                .Select(ToCandidate),
        ];

        List<CandidateTransferRow> transferRows =
        [
            .. transfers.Stored
                .Where(transfer => (accountIds.Contains(transfer.SourceAccountId)
                        || accountIds.Contains(transfer.DestinationAccountId))
                    && IsWithin(period, transfer.OccurredOn))
                .Select(transfer => new CandidateTransferRow(
                    transfer.Id,
                    transfer.OccurredOn,
                    transfer.Amount,
                    transfer.SourceAccountId,
                    transfer.DestinationAccountId)),
        ];

        return Task.FromResult(new CandidateSnapshot(transactionRows, transferRows));
    }

    public Task<ImportRunCreationsInUse> GetCreationsNamedOutsideRunAsync(
        Guid importRunId,
        CancellationToken cancellationToken)
    {
        CreationsRequestedForRunIds.Add(importRunId);
        TokensSeen.Add(cancellationToken);

        ImportRun run = runs.Stored.Single(candidate => candidate.Id == importRunId);

        HashSet<Guid> ownTransactionIds = [.. run.TransactionLinks.Select(link => link.TransactionId)];
        HashSet<Guid> ownTransferIds = [.. run.TransferLinks.Select(link => link.TransferId)];

        List<Transaction> outsideTransactions =
            [.. transactions.Stored.Where(transaction => !ownTransactionIds.Contains(transaction.Id))];
        List<Transfer> outsideTransfers =
            [.. transfers.Stored.Where(transfer => !ownTransferIds.Contains(transfer.Id))];

        List<Guid> accountsInUse =
        [
            .. run.CreatedAccountLinks
                .Select(link => link.AccountId)
                .Where(accountId =>
                    outsideTransactions.Any(transaction => transaction.AccountId == accountId)
                    || outsideTransfers.Any(transfer =>
                        transfer.SourceAccountId == accountId || transfer.DestinationAccountId == accountId)),
        ];

        List<Guid> categoriesInUse =
        [
            .. run.CreatedCategoryLinks
                .Select(link => link.CategoryId)
                .Where(categoryId =>
                    outsideTransactions.Any(transaction => transaction.CategoryId == categoryId)),
        ];

        return Task.FromResult(new ImportRunCreationsInUse(accountsInUse, categoriesInUse));
    }

    private static bool IsWithin(DateRange period, DateOnly day) => day >= period.From && day <= period.To;

    private CandidateTransactionRow ToCandidate(Transaction transaction)
    {
        Category category = categories.Stored.Single(candidate => candidate.Id == transaction.CategoryId);

        return new CandidateTransactionRow(
            transaction.Id,
            transaction.OccurredOn,
            transaction.Amount,
            transaction.AccountId,
            transaction.CategoryId,
            category.Kind,
            transaction.Note,
            transaction.ExternalRef);
    }
}
