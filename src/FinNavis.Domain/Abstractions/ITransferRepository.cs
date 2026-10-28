using FinNavis.Domain.Entities;

namespace FinNavis.Domain.Abstractions;

/// <summary>
/// A collection-like view of the <see cref="Transfer"/> aggregate. A repository port: the other side
/// of it is the data store, and Infrastructure supplies the adapter (sad.md §5).
/// </summary>
/// <remarks>
/// Write path only. Balances and history never load an entity — they go through
/// <c>ILedgerQueries</c> in the Application layer (ADR-0004). So there is no "list the transfers"
/// member here: a history page is a <c>UNION ALL</c> over two tables and returns read rows, not
/// entities (ADR-0001). The breakdown never reads transfers at all, which is what makes AC-20
/// structural.
/// </remarks>
public interface ITransferRepository
{
    /// <summary>
    /// Loads one transfer, or <c>null</c> when no transfer carries that id (AC-25). The entry point
    /// for an edit (AC-21) and a removal (AC-22).
    /// </summary>
    Task<Transfer?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Stages a new transfer. Nothing reaches PostgreSQL until the unit of work commits, and the
    /// commit writes one row — so a failed commit leaves neither account changed (AC-06, sad.md §6
    /// flow 2).
    /// </summary>
    void Add(Transfer transfer);

    /// <summary>
    /// Stages the removal of a transfer (AC-22). A hard delete: the entry disappears from history,
    /// and both balances it affected return to what they would have been, which costs nothing
    /// because no balance is stored (ADR-0002, ADR-0003).
    /// </summary>
    void Remove(Transfer transfer);
}
