using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.UnitTests.Fakes;

internal sealed class FakeAccountRepository : IAccountRepository
{
    private readonly Dictionary<Guid, Account> _stored = [];
    private readonly List<Account> _staged = [];
    private readonly List<Account> _stagedForRemoval = [];

    public List<CancellationToken> TokensSeen { get; } = [];

    public IReadOnlyCollection<Account> Stored => _stored.Values;

    public void Seed(params Account[] accounts)
    {
        foreach (Account account in accounts)
        {
            _stored[account.Id] = account;
        }
    }

    public Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        TokensSeen.Add(cancellationToken);

        return Task.FromResult(_stored.GetValueOrDefault(id));
    }

    public Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken)
    {
        TokensSeen.Add(cancellationToken);

        return Task.FromResult<IReadOnlyList<Account>>([.. _stored.Values]);
    }

    // Scoped as ux_accounts_name is: the name alone, and the account's own row counts.
    public Task<bool> ExistsWithNameAsync(string name, CancellationToken cancellationToken)
    {
        TokensSeen.Add(cancellationToken);

        return Task.FromResult(_stored.Values.Any(account => account.Name == name));
    }

    public void Add(Account account) => _staged.Add(account);

    public void Remove(Account account) => _stagedForRemoval.Add(account);

    public void Commit()
    {
        foreach (Account account in _staged)
        {
            _stored[account.Id] = account;
        }

        foreach (Account account in _stagedForRemoval)
        {
            _stored.Remove(account.Id);
        }

        _staged.Clear();
        _stagedForRemoval.Clear();
    }
}
