using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinNavis.Infrastructure.Repositories;

public sealed class EfAccountRepository : IAccountRepository
{
    private readonly FinNavisDbContext _dbContext;

    public EfAccountRepository(FinNavisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(Account account)
    {
        _dbContext.Accounts.Add(account);
    }

    public Task<bool> ExistsWithNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken)
    {
        return _dbContext.Accounts.AnyAsync(a => a.NormalizedName == normalizedName, cancellationToken);
    }

    public async Task<IReadOnlyList<Account>> ListOrderedByNameAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Accounts
            .AsNoTracking()
            .OrderBy(a => a.NormalizedName)
            .ToListAsync(cancellationToken);
    }

    public Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }
}
