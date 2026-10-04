using FinNavis.Application.Abstractions;
using FinNavis.Application.Common;

namespace FinNavis.Application.UnitTests.Fakes;

/// <summary>
/// Hand-written in-memory fake. Can be told to throw
/// <see cref="UniqueConstraintViolationException"/> on the next save, to exercise the race path.
/// </summary>
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    private bool _throwUniqueConstraintViolationOnNextSave;

    public int SaveChangesCallCount { get; private set; }

    public void ThrowUniqueConstraintViolationOnNextSave()
    {
        _throwUniqueConstraintViolationOnNextSave = true;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;

        if (_throwUniqueConstraintViolationOnNextSave)
        {
            _throwUniqueConstraintViolationOnNextSave = false;
            throw new UniqueConstraintViolationException("ix_accounts_normalized_name", new InvalidOperationException());
        }

        return Task.CompletedTask;
    }
}
