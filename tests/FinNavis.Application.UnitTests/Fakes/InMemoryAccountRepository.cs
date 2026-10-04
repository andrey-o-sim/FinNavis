using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.UnitTests.Fakes;

/// <summary>Hand-written in-memory fake. No mocking library is referenced in this repo.</summary>
internal sealed class InMemoryAccountRepository : IAccountRepository
{
    private readonly List<Account> _accounts = [];

    public IReadOnlyList<Account> Accounts => _accounts;

    public void Add(Account account)
    {
        _accounts.Add(account);
    }

    public Task<bool> ExistsWithNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken)
    {
        var exists = _accounts.Any(a => a.NormalizedName == normalizedName);

        return Task.FromResult(exists);
    }

    public Task<IReadOnlyList<Account>> ListOrderedByNameAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Account> ordered = [.. _accounts.OrderBy(a => a.NormalizedName, StringComparer.Ordinal)];

        return Task.FromResult(ordered);
    }

    public Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = _accounts.FirstOrDefault(a => a.Id == id);

        return Task.FromResult(account);
    }
}
