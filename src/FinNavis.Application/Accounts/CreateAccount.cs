using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Errors;

namespace FinNavis.Application.Accounts;

public sealed class CreateAccount(IAccountRepository accounts, IUnitOfWork unitOfWork, IClock clock)
{
    public async Task<Account> ExecuteAsync(string name, CancellationToken cancellationToken)
    {
        // Nothing is staged yet, so building the account first costs nothing and gives the check
        // the name the entity settled on — the stored name is trimmed.
        Account account = Account.Create(Guid.CreateVersion7(), name, clock.UtcNow);

        if (await accounts.ExistsWithNameAsync(account.Name, cancellationToken))
        {
            throw DuplicateName(account.Name);
        }

        accounts.Add(account);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return account;
    }

    private static DomainRuleViolationException DuplicateName(string name) =>
        new("Account.DuplicateName", $"An account named '{name}' already exists.");
}
