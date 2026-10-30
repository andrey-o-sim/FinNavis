using FinNavis.Domain.Entities;

namespace FinNavis.Domain.Abstractions;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(Transaction transaction);

    void Remove(Transaction transaction);
}
