using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinNavis.Infrastructure.Repositories;

public sealed class AccountRepository(FinNavisDbContext context) : IAccountRepository
{
    public Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Accounts.FirstOrDefaultAsync(account => account.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken) =>
        await context.Accounts
            .AsNoTracking()
            .OrderBy(account => account.Name)
            .ToListAsync(cancellationToken);

    // Scoped exactly as ux_accounts_name is scoped: the name alone, with no second column.
    public Task<bool> ExistsWithNameAsync(string name, CancellationToken cancellationToken) =>
        context.Accounts.AnyAsync(account => account.Name == name, cancellationToken);

    public void Add(Account account) => context.Accounts.Add(account);

    public void Remove(Account account) => context.Accounts.Remove(account);
}
