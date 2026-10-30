using FinNavis.Application.Abstractions;

namespace FinNavis.Infrastructure.Persistence;

public sealed class UnitOfWork(FinNavisDbContext context) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
