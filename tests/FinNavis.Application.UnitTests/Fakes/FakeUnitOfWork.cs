using FinNavis.Application.Abstractions;

namespace FinNavis.Application.UnitTests.Fakes;

/// <summary>
/// Counts commits and makes the staged changes of <see cref="FakeAccountRepository"/> visible,
/// so a test can tell "refused" from "written" without a database.
/// </summary>
internal sealed class FakeUnitOfWork(Action? onCommit = null) : IUnitOfWork
{
    public int Commits { get; private set; }

    /// <summary>Every token the use case passed in, so a test can check it was carried through.</summary>
    public List<CancellationToken> TokensSeen { get; } = [];

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        TokensSeen.Add(cancellationToken);
        Commits++;
        onCommit?.Invoke();

        return Task.CompletedTask;
    }
}
