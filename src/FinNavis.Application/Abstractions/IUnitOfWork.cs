using FinNavis.Application.Common;

namespace FinNavis.Application.Abstractions;

/// <summary>
/// Commits every change staged on repositories in this request as one transaction. The other
/// side is a database transaction.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves every staged change. Throws <see cref="UniqueConstraintViolationException"/> when
    /// the store rejects a duplicate that a use case's own check did not catch.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
