using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Errors;

namespace FinNavis.Application.Accounts;

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
