using FinNavis.Domain.Entities;

namespace FinNavis.Domain.Abstractions;

public interface ITransferRepository
{
    Task<Transfer?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(Transfer transfer);

    void Remove(Transfer transfer);
}
