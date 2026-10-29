using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinNavis.Infrastructure.Repositories;

/// <summary>
/// The EF Core adapter behind <see cref="ITransferRepository"/> (sad.md §5).
/// </summary>
/// <remarks>
/// Write path only. One row carries both sides, so a commit either writes the whole transfer or
/// none of it (AC-06, sad.md §6 flow 2).
/// </remarks>
public sealed class TransferRepository(FinNavisDbContext context) : ITransferRepository
{
    public Task<Transfer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Transfers.FirstOrDefaultAsync(transfer => transfer.Id == id, cancellationToken);

    public void Add(Transfer transfer) => context.Transfers.Add(transfer);

    public void Remove(Transfer transfer) => context.Transfers.Remove(transfer);
}
