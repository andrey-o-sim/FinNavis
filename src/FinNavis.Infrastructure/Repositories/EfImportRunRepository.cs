using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinNavis.Infrastructure.Repositories;

public sealed class EfImportRunRepository(FinNavisDbContext context) : IImportRunRepository
{
    // Tracked, because undo marks the run and commits it in the same unit of work. Split, so the
    // five child collections arrive as one query each instead of one row per combination.
    public Task<ImportRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.ImportRuns
            .Include(run => run.TransactionLinks)
            .Include(run => run.TransferLinks)
            .Include(run => run.CreatedAccountLinks)
            .Include(run => run.CreatedCategoryLinks)
            .Include(run => run.SetAsideRows)
            .AsSplitQuery()
            .FirstOrDefaultAsync(run => run.Id == id, cancellationToken);

    public void Add(ImportRun run) => context.ImportRuns.Add(run);
}
