using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Errors;

namespace FinNavis.Application.Accounts;

/// <summary>
/// Opens an account under a name the owner chose (AC-07, AC-35, sad.md §6 flow 13).
/// </summary>
/// <remarks>
/// <b>The new account has no balance of its own.</b> AC-07's zero is the sum of no movements
/// (ADR-0002), so nothing is written to make it zero and nothing could make it wrong.
/// <para>
/// <b>The name check is not the guarantee — the unique index is.</b> The check is here so the
/// owner reads the refusal AC-35 words rather than a constraint violation; a race between two
/// writes still fails at the database instead of creating a duplicate (sad.md §6 flow 13).
/// </para>
/// </remarks>
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
