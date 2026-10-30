using FinNavis.Domain.Abstractions;

namespace FinNavis.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
