using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.UnitTests.Fakes;

internal sealed class FakeTransactionRepository : ITransactionRepository
{
    private readonly Dictionary<Guid, Transaction> _stored = [];
    private readonly Dictionary<Guid, Snapshot> _committed = [];
    private readonly List<Transaction> _staged = [];
    private readonly List<Transaction> _stagedForRemoval = [];

    public List<CancellationToken> TokensSeen { get; } = [];

    public IReadOnlyCollection<Transaction> Stored => _stored.Values;

    public void Seed(params Transaction[] transactions)
    {
        foreach (Transaction transaction in transactions)
        {
            _stored[transaction.Id] = transaction;
            _committed[transaction.Id] = Snapshot.Of(transaction);
        }
    }

    public Snapshot Committed(Guid transactionId) => _committed[transactionId];

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        TokensSeen.Add(cancellationToken);

        return Task.FromResult(_stored.GetValueOrDefault(id));
    }

    public void Add(Transaction transaction) => _staged.Add(transaction);

    public void Remove(Transaction transaction) => _stagedForRemoval.Add(transaction);

    public void Commit()
    {
        foreach (Transaction transaction in _staged)
        {
            _stored[transaction.Id] = transaction;
        }

        foreach (Transaction transaction in _stagedForRemoval)
        {
            _stored.Remove(transaction.Id);
            _committed.Remove(transaction.Id);
        }

        _staged.Clear();
        _stagedForRemoval.Clear();

        foreach (Transaction transaction in _stored.Values)
        {
            _committed[transaction.Id] = Snapshot.Of(transaction);
        }
    }

    internal readonly record struct Snapshot(
        Guid AccountId,
        Guid CategoryId,
        decimal Amount,
        DateOnly OccurredOn,
        string? Note)
    {
        public static Snapshot Of(Transaction transaction) =>
            new(
                transaction.AccountId,
                transaction.CategoryId,
                transaction.Amount.Amount,
                transaction.OccurredOn,
                transaction.Note);
    }
}
