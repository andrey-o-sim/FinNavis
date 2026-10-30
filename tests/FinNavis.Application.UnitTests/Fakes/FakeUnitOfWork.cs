using FinNavis.Application.Abstractions;

namespace FinNavis.Application.UnitTests.Fakes;

internal sealed class FakeUnitOfWork(Action? onCommit = null) : IUnitOfWork
{
    public int Commits { get; private set; }

    public List<CancellationToken> TokensSeen { get; } = [];

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        TokensSeen.Add(cancellationToken);
        Commits++;
        onCommit?.Invoke();

        return Task.CompletedTask;
    }
}
