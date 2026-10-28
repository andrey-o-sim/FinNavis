using FinNavis.Domain.Entities;

namespace FinNavis.Domain.Abstractions;

/// <summary>
/// A collection-like view of the <see cref="Transaction"/> aggregate. A repository port: the other
/// side of it is the data store, and Infrastructure supplies the adapter (sad.md §5).
/// </summary>
/// <remarks>
/// Write path only. Balances, history and the breakdown never load an entity — they go through
/// <c>ILedgerQueries</c> in the Application layer (ADR-0004). So there is no "list the
/// transactions" member here: a history page is a <c>UNION ALL</c> over two tables and returns
/// read rows, not entities (ADR-0001).
/// </remarks>
public interface ITransactionRepository
{
    /// <summary>
    /// Loads one transaction, or <c>null</c> when no transaction carries that id (AC-25). The
    /// entry point for an edit (AC-21) and a removal (AC-22).
    /// </summary>
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Stages a new transaction. Nothing reaches PostgreSQL until the unit of work commits.
    /// </summary>
    void Add(Transaction transaction);

    /// <summary>
    /// Stages the removal of a transaction (AC-22). A hard delete: the entry disappears from
    /// history, and every balance it affected returns to what it would have been, which costs
    /// nothing because no balance is stored (ADR-0002, ADR-0003).
    /// </summary>
    void Remove(Transaction transaction);
}
