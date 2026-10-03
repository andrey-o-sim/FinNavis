using FinNavis.Domain.Entities;

namespace FinNavis.Domain.Abstractions;

/// <summary>
/// A collection-like view of the accounts. The other side is the data store, so this is a port.
/// </summary>
/// <remarks>
/// Domain declares it and never calls it. Use cases in Application do the fetching, and
/// Infrastructure supplies the adapter. Only BCL types appear here: Domain takes no packages.
/// Nothing on this port saves. Committing is <c>IUnitOfWork</c>, which lives in Application.
/// </remarks>
public interface IAccountRepository
{
    /// <summary>Stages a new account. Does not commit.</summary>
    Task AddAsync(Account account, CancellationToken cancellationToken);

    /// <summary>Returns the account with this id, or <c>null</c> when there is none.</summary>
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Returns every account, ordered by name.</summary>
    Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken);
}
