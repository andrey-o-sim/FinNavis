using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinNavis.Infrastructure.Repositories;

public sealed class TransactionRepository(FinNavisDbContext context) : ITransactionRepository
{
    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Transactions.FirstOrDefaultAsync(
            transaction => transaction.Id == id,
            cancellationToken);

    public void Add(Transaction transaction) => context.Transactions.Add(transaction);

    public void Remove(Transaction transaction) => context.Transactions.Remove(transaction);
}
