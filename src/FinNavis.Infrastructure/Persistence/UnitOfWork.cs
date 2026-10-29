using FinNavis.Application.Abstractions;

namespace FinNavis.Infrastructure.Persistence;

/// <summary>
/// The EF Core adapter behind <see cref="IUnitOfWork"/> (sad.md §5).
/// </summary>
/// <remarks>
/// Every repository adapter shares the one <see cref="FinNavisDbContext"/> this holds, so the
/// changes they staged are the changes this commits.
/// <para>
/// <b>One transaction per commit.</b> <c>SaveChangesAsync</c> wraps the whole batch of statements
/// in a database transaction, so a statement that fails rolls back the ones before it — nothing is
/// half-written (AC-06, sad.md §6 flow 2). A caller that opened its own transaction keeps it; EF
/// Core joins that one instead of starting a second.
/// </para>
/// </remarks>
public sealed class UnitOfWork(FinNavisDbContext context) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
