using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.UnitTests.Fakes;

internal sealed class FakeImportRunRepository : IImportRunRepository
{
    private readonly Dictionary<Guid, ImportRun> _stored = [];
    private readonly List<ImportRun> _staged = [];

    public List<Guid> RequestedIds { get; } = [];

    public List<CancellationToken> TokensSeen { get; } = [];

    public IReadOnlyCollection<ImportRun> Stored => _stored.Values;

    public IReadOnlyCollection<ImportRun> Staged => _staged;

    public void Seed(params ImportRun[] runs)
    {
        foreach (ImportRun run in runs)
        {
            _stored[run.Id] = run;
        }
    }

    public Task<ImportRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        RequestedIds.Add(id);
        TokensSeen.Add(cancellationToken);

        return Task.FromResult(_stored.GetValueOrDefault(id));
    }

    public void Add(ImportRun run) => _staged.Add(run);
}
