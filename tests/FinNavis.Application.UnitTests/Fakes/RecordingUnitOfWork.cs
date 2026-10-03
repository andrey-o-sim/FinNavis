using FinNavis.Application.Abstractions;

namespace FinNavis.Application.UnitTests.Fakes;

/// <summary>
/// Hand-rolled <see cref="IUnitOfWork"/> that counts commits, so a test can prove a failed
/// use case committed nothing.
/// </summary>
internal sealed class RecordingUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;

        return Task.CompletedTask;
    }
}
