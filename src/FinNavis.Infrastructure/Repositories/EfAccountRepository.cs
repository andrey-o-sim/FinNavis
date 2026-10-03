using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinNavis.Infrastructure.Repositories;

/// <summary>
/// Adapter for <see cref="IAccountRepository"/> over EF Core.
/// </summary>
internal sealed class EfAccountRepository(FinNavisDbContext context) : IAccountRepository
{
    public async Task AddAsync(Account account, CancellationToken cancellationToken)
    {
        // Stages only. IUnitOfWork commits, so a use case can add several things in one go.
        await context.Accounts.AddAsync(account, cancellationToken);
    }

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Accounts.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken)
    {
        // No index for this sort, and that is the decision, not an oversight. This is a
        // single-user table with tens of rows, read in full with no filter. An index on name
        // would be write cost for nothing. See docs/adr/0002-accounts-slice.md.
        return await context.Accounts
            .AsNoTracking()
            .OrderBy(account => account.Name)
            .ToListAsync(cancellationToken);
    }
}
