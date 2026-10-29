using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Errors;

namespace FinNavis.Application.Accounts;

/// <summary>
/// Removes an account the ledger has nothing recorded against (AC-09, AC-10, AC-25,
/// sad.md §6 flow 5).
/// </summary>
/// <remarks>
/// <b>Recorded history blocks the removal rather than being deleted with it.</b> The foreign keys
/// carry no cascade on purpose, so a movement can never be orphaned or silently taken away
/// (<c>data-model.md</c>).
/// <para>
/// <b>The entity cannot answer this itself</b> — an account cannot see its own history, so the
/// question goes to <c>ILedgerQueries</c>, which asks whether a row exists and never loads one.
/// </para>
/// </remarks>
public sealed class DeleteAccount(
    IAccountRepository accounts,
    ILedgerQueries ledger,
    IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(Guid accountId, CancellationToken cancellationToken)
    {
        Account account = await accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw NotFound(accountId);

        if (await ledger.AnyMovementNamesAccountAsync(accountId, cancellationToken))
        {
            throw HasRecordedHistory();
        }

        accounts.Remove(account);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static DomainRuleViolationException NotFound(Guid accountId) =>
        new("Account.NotFound", $"No account in the ledger has id {accountId}.");

    private static DomainRuleViolationException HasRecordedHistory() =>
        new(
            "Account.HasRecordedHistory",
            "Recorded history depends on this account, so it cannot be deleted.");
}
