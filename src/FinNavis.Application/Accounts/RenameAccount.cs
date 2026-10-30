using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Errors;

namespace FinNavis.Application.Accounts;

public sealed class RenameAccount(IAccountRepository accounts, IUnitOfWork unitOfWork)
{
    public async Task<Account> ExecuteAsync(Guid accountId, string name, CancellationToken cancellationToken)
    {
        Account account = await accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw NotFound(accountId);

        // The stored name is trimmed, so the comparison and the check have to use a trimmed name.
        string requestedName = (name ?? string.Empty).Trim();

        // The check answers true for the account's own row, so keeping its current name is not a
        // clash — the refusal is about another account already carrying the name.
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
