using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Infrastructure.Persistence;

namespace FinNavis.Infrastructure.Repositories;

public sealed class EfImportRunRepository : IImportRunRepository
{
    public EfImportRunRepository(FinNavisDbContext context) => throw new NotImplementedException();

    public Task<ImportRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public void Add(ImportRun run) => throw new NotImplementedException();
}
