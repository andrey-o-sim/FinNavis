using FinNavis.Domain.Entities;

namespace FinNavis.Application.Features.Accounts;

/// <summary>
/// An account as a use case returns it. Presentation maps this to its own response record.
/// </summary>
public sealed record AccountDto(
    Guid Id,
    string Name,
    string Type,
    decimal InitialBalance,
    decimal Balance,
    DateTimeOffset CreatedAt)
{
    // Transactions do not exist yet, so the total is always zero. This is the only place
    // that zero is written. When transactions arrive, the total comes from a query and
    // this constant goes away.
    private const decimal NoTransactionsYet = 0m;

    public static AccountDto FromAccount(Account account)
    {
        return new AccountDto(
            account.Id,
            account.Name,
            account.Type.ToString(),
            account.InitialBalance,
            account.CalculateBalance(NoTransactionsYet),
            account.CreatedAt);
    }
}
