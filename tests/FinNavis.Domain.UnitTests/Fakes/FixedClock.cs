using FinNavis.Domain.Abstractions;

namespace FinNavis.Domain.UnitTests.Fakes;

/// <summary>Always returns the same instant. <see cref="Offset"/> is always zero, like the real clock.</summary>
internal sealed class FixedClock : IClock
{
    public FixedClock(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; }
}
