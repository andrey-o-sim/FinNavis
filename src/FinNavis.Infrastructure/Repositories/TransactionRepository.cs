using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinNavis.Infrastructure.Repositories;

/// <summary>
/// The EF Core adapter behind <see cref="ITransactionRepository"/> (sad.md §5).
/// </summary>
/// <remarks>
/// Write path only. There is no "list the transactions" member, because a history page is a
/// <c>UNION ALL</c> over two tables behind <c>ILedgerQueries</c> and returns read rows, not
/// entities (ADR-0001, ADR-0004).
/// </remarks>
public sealed class TransactionRepository(FinNavisDbContext context) : ITransactionRepository
{
    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Transactions.FirstOrDefaultAsync(
            transaction => transaction.Id == id,
            cancellationToken);

    public void Add(Transaction transaction) => context.Transactions.Add(transaction);

    public void Remove(Transaction transaction) => context.Transactions.Remove(transaction);
}
