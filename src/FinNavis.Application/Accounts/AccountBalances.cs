using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Accounts;

/// <summary>
/// What every account holds, and the total across them.
/// </summary>
/// <param name="Accounts">One figure per account. Empty only when the ledger has no account.</param>
/// <param name="Total">
/// The per-account figures added together. A transfer between the owner's own accounts leaves it
/// unchanged by construction, because the same amount leaves one row and arrives in another
/// (AC-15).
/// </param>
public sealed record AccountBalances(IReadOnlyList<AccountBalanceRow> Accounts, Money Total);
