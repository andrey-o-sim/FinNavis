using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinNavis.Infrastructure.Repositories;

public sealed class TransferRepository(FinNavisDbContext context) : ITransferRepository
{
    public Task<Transfer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Transfers.FirstOrDefaultAsync(transfer => transfer.Id == id, cancellationToken);

    public void Add(Transfer transfer) => context.Transfers.Add(transfer);

    public void Remove(Transfer transfer) => context.Transfers.Remove(transfer);
}
