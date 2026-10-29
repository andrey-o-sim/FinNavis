using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Accounts;

/// <summary>
/// Reads the balance of every account, or of one account, and adds the figures together
/// (AC-14, AC-15, AC-25, sad.md §6 flow 7).
/// </summary>
/// <remarks>
/// <b>Nothing is stored and nothing is cached</b> (ADR-0002). Every figure is summed from the
/// movements at read time, so an edited or removed entry needs no correction step (AC-21, AC-22).
/// <para>
/// <b>The total is added up here rather than in SQL.</b> The query returns one row per account, so
/// a transfer between the owner's own accounts cancels itself out in this sum by construction —
/// there is no second query that could disagree with the per-account figures (AC-15).
/// </para>
/// </remarks>
public sealed class GetAccountBalances(ILedgerQueries ledger)
{
    public async Task<AccountBalances> ExecuteAsync(Guid? accountId, CancellationToken cancellationToken)
    {
        IReadOnlyList<AccountBalanceRow> rows =
            await ledger.GetAccountBalancesAsync(accountId, cancellationToken);

        // AC-25: an account the ledger does not hold has no row, and no balance is invented for it.
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
