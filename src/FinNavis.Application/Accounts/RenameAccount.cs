using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Errors;

namespace FinNavis.Application.Accounts;

/// <summary>
/// Gives an account a new name and keeps its identity (AC-08, AC-25, AC-35).
/// </summary>
/// <remarks>
/// <b>Nothing is rewritten.</b> Every movement points at the account by id, never by name, so the
/// recorded history, the balances and the breakdown follow the new name on their own (AC-08).
/// <para>
/// <b>Every check runs before the account is touched</b>, so a refused rename leaves the row
/// exactly as it was.
/// </para>
/// </remarks>
public sealed class RenameAccount(IAccountRepository accounts, IUnitOfWork unitOfWork)
{
    public async Task<Account> ExecuteAsync(Guid accountId, string name, CancellationToken cancellationToken)
    {
        Account account = await accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw NotFound(accountId);

        // The stored name is trimmed, so the comparison and the check have to use a trimmed name.
        string requestedName = (name ?? string.Empty).Trim();

        // The check answers true for the account's own row, so keeping its current name is not a
        // clash — AC-35 is about another account already carrying the name.
        bool nameChanges = !string.Equals(requestedName, account.Name, StringComparison.Ordinal);

        if (nameChanges && await accounts.ExistsWithNameAsync(requestedName, cancellationToken))
        {
            throw DuplicateName(requestedName);
        }

        account.Rename(requestedName);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return account;
    }

    private static DomainRuleViolationException NotFound(Guid accountId) =>
        new("Account.NotFound", $"No account in the ledger has id {accountId}.");

    private static DomainRuleViolationException DuplicateName(string name) =>
        new("Account.DuplicateName", $"An account named '{name}' already exists.");
}
