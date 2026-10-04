using FinNavis.Domain.Abstractions;

namespace FinNavis.Infrastructure.IntegrationTests.Fakes;

/// <summary>A clock a test can set to a known instant. <see cref="UtcNow"/>'s offset is always zero.</summary>
public sealed class MutableClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
}
