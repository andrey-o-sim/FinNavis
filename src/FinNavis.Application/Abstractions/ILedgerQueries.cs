using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Abstractions;

/// <summary>
/// The read path of the ledger. Every figure the owner reads — balances, history, the breakdown —
/// comes from here, and from nowhere else.
/// </summary>
/// <remarks>
/// A port: the other side of it is PostgreSQL, and Infrastructure supplies the adapter (sad.md §5).
/// It lives in Application because a report is what a use case needs from the outside world, not
/// part of the domain model (ADR-0004).
/// <para>
/// <b>No entity is ever loaded here.</b> Aggregation runs in PostgreSQL and only result rows travel
/// back, which is the whole point of ADR-0004: the breakdown would otherwise pull ~20 000
/// <c>Transaction</c> entities into memory to sum them, and that cost grows every year. Writes keep
/// going through the five repository ports in <c>Domain/Abstractions</c>, which keep returning
/// entities.
/// </para>
/// <para>
/// <b>Nothing is stored and nothing is cached</b> (ADR-0002). Every figure is computed from the
/// movements at read time, which is what makes an edit or a removal free — there is no stored total
/// to correct (AC-21, AC-22).
/// </para>
/// <para>
/// <b>Filters are passed through, not validated here.</b> A bad period is refused by the use case
/// as <c>History.InvalidFilter</c> or <c>Breakdown.InvalidPeriod</c>, and an unknown account as
/// <c>Account.NotFound</c> (AC-25), before any of these is called.
/// </para>
/// </remarks>
public interface ILedgerQueries
{
    /// <summary>
    /// The balance of every account, or of one account when <paramref name="accountId"/> is given
    /// (AC-14, sad.md §6 flow 7).
    /// </summary>
    /// <param name="accountId">The one account to read, or <c>null</c> for every account.</param>
    /// <remarks>
    /// A three-part sum (ADR-0001): the transactions on the account, minus the transfers out of it,
    /// plus the transfers into it. Transactions join <c>categories</c> to learn whether a row adds
    /// or subtracts, because the amount is always positive and direction comes from the category's
    /// kind (<c>data-model.md</c> decision 3).
    /// <para>
    /// Not paginated. <c>accounts</c> holds a handful of rows, and flow 7 reads them in one query.
    /// </para>
    /// <para>
    /// The total across all accounts is <b>not</b> returned. The use case adds the rows together,
    /// so a transfer between the owner's own accounts leaves it unchanged by construction (AC-15).
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<AccountBalanceRow>> GetAccountBalancesAsync(
        Guid? accountId,
        CancellationToken cancellationToken);

    /// <summary>
    /// True when any transaction or transfer names this account. Asked before a delete, because
    /// recorded history is what refuses it (AC-09, sad.md §6 flow 5).
    /// </summary>
    /// <remarks>
    /// A transfer counts from either side. The check only asks whether a row exists, so it never
    /// aggregates and never loads an entity.
    /// </remarks>
    Task<bool> AnyMovementNamesAccountAsync(Guid accountId, CancellationToken cancellationToken);

    /// <summary>
    /// True when any transaction names this category. Asked before a delete, for the same reason
    /// (AC-12, sad.md §6 flow 6).
    /// </summary>
    /// <remarks>
    /// Only <c>transactions</c> is read. A transfer carries no category, so it can never name one.
    /// <para>
    /// There is no matching check for a category group: removing a group is allowed, because no
    /// movement names a group — the categories keep their history and only lose the link (AC-29).
    /// </para>
    /// </remarks>
    Task<bool> AnyTransactionNamesCategoryAsync(Guid categoryId, CancellationToken cancellationToken);

    /// <summary>
    /// One page of recorded movements, transactions and transfers together, newest first
    /// (AC-16, AC-17, sad.md §6 flow 8).
    /// </summary>
    /// <param name="filter">What to narrow down to. Every part of it is optional.</param>
    /// <param name="page">Which page to read, and how large it is.</param>
    /// <remarks>
    /// One <c>UNION ALL</c> over <c>transactions</c> and <c>transfers</c>, paged by keyset over
    /// <c>(occurred_on, id)</c> — <c>id</c> is a time-ordered UUID v7 (ADR-0005), which is what
    /// makes it a stable tiebreaker inside one day across both tables.
    /// <para>
    /// A period that recorded nothing comes back as an empty page, never as a failure (AC-17).
    /// </para>
    /// </remarks>
    Task<HistoryPage> GetHistoryPageAsync(
        HistoryFilter filter,
        HistoryPageRequest page,
        CancellationToken cancellationToken);

    /// <summary>
    /// The period's totals, one row per category that recorded something, each carrying the group
    /// it sits in right now (AC-18, AC-19, AC-20, sad.md §6 flow 3).
    /// </summary>
    /// <param name="period">The days to total. Both ends count as inside.</param>
    /// <param name="accountId">
    /// Total only the transactions recorded on this account, or <c>null</c> for every account.
    /// </param>
    /// <remarks>
    /// One aggregate query over <c>transactions</c>, joined to the current category-to-group
    /// mapping. <c>transfers</c> is never read, so no transfer can be counted as spending or as
    /// earning (AC-20, ADR-0001).
    /// <para>
    /// Rows come back raw. Grouping them, totalling each side and assembling the Default group
    /// bucket is the use case's job (<c>BreakdownCategoryRow</c>), and a group that recorded
    /// nothing in the period simply has no row — it is left out of the report rather than reported
    /// as zero (api-forge gate).
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<BreakdownCategoryRow>> GetBreakdownAsync(
        DateRange period,
        Guid? accountId,
        CancellationToken cancellationToken);
}
