using FinNavis.Domain.Entities;

namespace FinNavis.Domain.Abstractions;

/// <summary>
/// Collection-like view of the account aggregate. The other side is the data store.
/// </summary>
public interface IAccountRepository
{
    /// <summary>
    /// Stages a new account. Persistence happens when the caller commits through
    /// <c>IUnitOfWork.SaveChangesAsync</c>; this method does not save by itself.
    /// </summary>
    void Add(Account account);

    Task<bool> ExistsWithNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken);

    Task<IReadOnlyList<Account>> ListOrderedByNameAsync(CancellationToken cancellationToken);

    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
