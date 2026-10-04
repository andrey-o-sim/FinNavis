using FinNavis.Application.Abstractions;
using FinNavis.Application.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinNavis.Infrastructure.Persistence;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly FinNavisDbContext _dbContext;

    public EfUnitOfWork(FinNavisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" } postgresException)
        {
            throw new UniqueConstraintViolationException(postgresException.ConstraintName ?? "unknown", ex);
        }
    }
}
