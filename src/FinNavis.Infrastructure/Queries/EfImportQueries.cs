using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.ValueObjects;
using FinNavis.Infrastructure.Persistence;

namespace FinNavis.Infrastructure.Queries;

public sealed class EfImportQueries : IImportQueries
{
    public EfImportQueries(FinNavisDbContext context) => throw new NotImplementedException();

    public Task<CandidateSnapshot> GetCandidateSnapshotAsync(
        IReadOnlyCollection<Guid> accountIds,
        DateRange period,
        CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<ImportRunCreationsInUse> GetCreationsNamedOutsideRunAsync(
        Guid importRunId,
        CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
