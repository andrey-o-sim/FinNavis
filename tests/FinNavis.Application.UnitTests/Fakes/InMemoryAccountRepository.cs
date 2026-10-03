using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.UnitTests.Fakes;

/// <summary>
/// Hand-rolled <see cref="IAccountRepository"/>. No mocking library: the port has three
/// members, and we assert on state rather than on calls.
/// </summary>
internal sealed class InMemoryAccountRepository : IAccountRepository
{
    /// <summary>Everything staged so far, in insertion order.</summary>
    public List<Account> Items { get; } = [];

    public Task AddAsync(Account account, CancellationToken cancellationToken)
    {
        Items.Add(account);

        return Task.CompletedTask;
    }

    public Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(Items.SingleOrDefault(account => account.Id == id));
    }

    public Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Account> ordered = Items
            .OrderBy(account => account.Name, StringComparer.Ordinal)
            .ToArray();

        return Task.FromResult(ordered);
    }
}
