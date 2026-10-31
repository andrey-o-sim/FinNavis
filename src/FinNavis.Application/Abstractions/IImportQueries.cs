using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Abstractions;

public interface IImportQueries
{
    // Taken once, before the run writes anything, so it can never hold a movement the run itself
    // recorded. Two identical rows in one file are then both recorded, as two real spends.
    Task<CandidateSnapshot> GetCandidateSnapshotAsync(
        IReadOnlyCollection<Guid> accountIds,
        DateRange period,
        CancellationToken cancellationToken);

    Task<ImportRunCreationsInUse> GetCreationsNamedOutsideRunAsync(
        Guid importRunId,
        CancellationToken cancellationToken);
}
