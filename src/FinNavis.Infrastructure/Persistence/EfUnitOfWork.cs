using FinNavis.Application.Abstractions;

namespace FinNavis.Infrastructure.Persistence;

/// <summary>
/// Adapter for <see cref="IUnitOfWork"/> over EF Core, which is already a unit of work.
/// </summary>
internal sealed class EfUnitOfWork(FinNavisDbContext context) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
