using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Accounts;

public sealed class GetAccountBalances(ILedgerQueries ledger)
{
    public async Task<AccountBalances> ExecuteAsync(Guid? accountId, CancellationToken cancellationToken)
    {
        IReadOnlyList<AccountBalanceRow> rows =
            await ledger.GetAccountBalancesAsync(accountId, cancellationToken);

        // An account the ledger does not hold has no row, and no balance is invented for it.
        if (accountId is Guid requestedId && rows.Count == 0)
        {
            throw NotFound(requestedId);
        }

        Money total = rows.Aggregate(Money.Zero, (sum, row) => sum + row.Balance);

        return new AccountBalances(rows, total);
    }

    private static DomainRuleViolationException NotFound(Guid accountId) =>
        new("Account.NotFound", $"No account in the ledger has id {accountId}.");
}
