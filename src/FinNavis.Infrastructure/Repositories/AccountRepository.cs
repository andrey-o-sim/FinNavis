using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinNavis.Infrastructure.Repositories;

/// <summary>
/// The EF Core adapter behind <see cref="IAccountRepository"/> (sad.md §5).
/// </summary>
/// <remarks>
/// Write path only. Nothing here reaches PostgreSQL until <c>IUnitOfWork</c> commits.
/// <para>
/// A method that hands back an entity the caller may change keeps it tracked; a method that only
/// answers a question does not need tracking at all.
/// </para>
/// </remarks>
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
