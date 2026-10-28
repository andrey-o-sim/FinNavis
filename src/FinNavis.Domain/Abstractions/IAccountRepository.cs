using FinNavis.Domain.Entities;

namespace FinNavis.Domain.Abstractions;

/// <summary>
/// A collection-like view of the <see cref="Account"/> aggregate. A repository port: the other
/// side of it is the data store, and Infrastructure supplies the adapter (sad.md §5).
/// </summary>
/// <remarks>
/// Write path only. Balances, history and the breakdown never load an entity — they go through
/// <c>ILedgerQueries</c> in the Application layer (ADR-0004).
/// </remarks>
public interface IAccountRepository
{
    /// <summary>Loads one account, or <c>null</c> when no account carries that id (AC-25).</summary>
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Loads every account. The table holds a handful of rows, so there is no paging
    /// (<c>data-model.md</c>, access patterns).
    /// </summary>
    Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Answers whether any account already carries this name (AC-35, sad.md §6 flow 13).
    /// </summary>
    /// <remarks>
    /// Scoped exactly as <c>ux_accounts_name</c> is scoped — a name is unique on its own, with no
    /// second column. A rename to the account's own current name therefore answers <c>true</c>;
    /// the use case decides what that means.
    /// </remarks>
    Task<bool> ExistsWithNameAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Stages a new account. Nothing reaches PostgreSQL until the unit of work commits.
    /// </summary>
    void Add(Account account);

    /// <summary>
    /// Stages the removal of an account (AC-10). The caller has already checked that no movement
    /// names it (AC-09) — the entity cannot see its own history.
    /// </summary>
    void Remove(Account account);
}
