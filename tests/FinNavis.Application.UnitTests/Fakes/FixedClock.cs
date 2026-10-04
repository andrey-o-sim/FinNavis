using FinNavis.Domain.Abstractions;

namespace FinNavis.Application.UnitTests.Fakes;

/// <summary>Always returns the same instant.</summary>
internal sealed class FixedClock : IClock
{
    public FixedClock(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; }
}
