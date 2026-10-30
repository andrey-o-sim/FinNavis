using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.UnitTests.Fakes;

internal sealed class FakeTransferRepository : ITransferRepository
{
    private readonly Dictionary<Guid, Transfer> _stored = [];
    private readonly Dictionary<Guid, Snapshot> _committed = [];
    private readonly List<Transfer> _staged = [];
    private readonly List<Transfer> _stagedForRemoval = [];

    public List<CancellationToken> TokensSeen { get; } = [];

    public IReadOnlyCollection<Transfer> Stored => _stored.Values;

    public void Seed(params Transfer[] transfers)
    {
        foreach (Transfer transfer in transfers)
        {
            _stored[transfer.Id] = transfer;
            _committed[transfer.Id] = Snapshot.Of(transfer);
        }
    }

    public Snapshot Committed(Guid transferId) => _committed[transferId];

    public Task<Transfer?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        TokensSeen.Add(cancellationToken);

        return Task.FromResult(_stored.GetValueOrDefault(id));
    }

    public void Add(Transfer transfer) => _staged.Add(transfer);

    public void Remove(Transfer transfer) => _stagedForRemoval.Add(transfer);

    public void Commit()
    {
        foreach (Transfer transfer in _staged)
        {
            _stored[transfer.Id] = transfer;
        }

        foreach (Transfer transfer in _stagedForRemoval)
        {
            _stored.Remove(transfer.Id);
            _committed.Remove(transfer.Id);
        }

        _staged.Clear();
        _stagedForRemoval.Clear();

        foreach (Transfer transfer in _stored.Values)
        {
            _committed[transfer.Id] = Snapshot.Of(transfer);
        }
    }

    internal readonly record struct Snapshot(
        Guid SourceAccountId,
        Guid DestinationAccountId,
        decimal Amount,
        DateOnly OccurredOn)
    {
        public static Snapshot Of(Transfer transfer) =>
            new(
                transfer.SourceAccountId,
                transfer.DestinationAccountId,
                transfer.Amount.Amount,
                transfer.OccurredOn);
    }
}
