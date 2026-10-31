using FinNavis.Domain.Entities;

namespace FinNavis.Domain.Abstractions;

public interface IImportRunRepository
{
    Task<ImportRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(ImportRun run);
}
