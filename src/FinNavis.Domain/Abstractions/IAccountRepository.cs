using FinNavis.Domain.Entities;

namespace FinNavis.Domain.Abstractions;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsWithNameAsync(string name, CancellationToken cancellationToken);

    void Add(Account account);

    void Remove(Account account);
}
