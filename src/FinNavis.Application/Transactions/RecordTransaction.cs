using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Transactions;

/// <summary>
/// Records one movement on one account, filed under one category (AC-01, AC-03, AC-25,
/// sad.md §6 flow 1).
/// </summary>
/// <remarks>
/// <b>An unknown account or category is named back and nothing is created</b> — not the entry, and
/// not the category the owner mistyped (AC-03). Both lookups run before the entry is built, so a
/// refused recording leaves the ledger exactly as it was.
/// <para>
/// <b>The balance is not written anywhere.</b> It falls by the amount on the next read, because
/// every balance is summed from the movements at read time (ADR-0002). There is no stored figure
/// that could disagree with the entry.
/// </para>
/// <para>
/// <b>The amount stays positive and direction is not stored.</b> The category's side is what makes
/// the entry add or subtract (<c>data-model.md</c> decision 3), so the entity refuses a
/// non-positive amount (AC-02) rather than accepting a signed one.
/// </para>
/// <para>
/// <b><c>external_ref</c> is left null.</b> Nothing in this feature sets it — csv-import fills it
/// later (ADR-0006).
/// </para>
/// </remarks>
public sealed class RecordTransaction(
    ITransactionRepository transactions,
    IAccountRepository accounts,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Transaction> ExecuteAsync(
        Guid accountId,
        Guid categoryId,
        decimal amount,
        DateOnly occurredOn,
        string? note,
        CancellationToken cancellationToken)
    {
        await RequireAccountAsync(accountId, cancellationToken);
        await RequireCategoryAsync(categoryId, cancellationToken);

        Transaction transaction = Transaction.Record(
            Guid.CreateVersion7(),
            accountId,
            categoryId,
            new Money(amount),
            occurredOn,
            note,
            clock.UtcNow);

        transactions.Add(transaction);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return transaction;
    }

    private async Task RequireAccountAsync(Guid accountId, CancellationToken cancellationToken) =>
        _ = await accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw TransactionErrors.AccountNotFound(accountId);

    private async Task RequireCategoryAsync(Guid categoryId, CancellationToken cancellationToken) =>
        _ = await categories.GetByIdAsync(categoryId, cancellationToken)
            ?? throw TransactionErrors.CategoryNotFound(categoryId);
}
