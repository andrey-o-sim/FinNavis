using FinNavis.Domain.ValueObjects;

namespace FinNavis.Domain.UnitTests.ValueObjects;

public sealed class DateRangeTests
{
    private static readonly DateOnly First = new(2026, 3, 1);
    private static readonly DateOnly Last = new(2026, 3, 31);

    [Fact]
    public void Contains_FirstDay_IsTrue()
    {
        DateRange range = DateRange.Create(First, Last);

        range.Contains(First).Should().BeTrue();
    }

    [Fact]
    public void Contains_LastDay_IsTrue()
    {
        DateRange range = DateRange.Create(First, Last);

        range.Contains(Last).Should().BeTrue();
    }

    [Fact]
    public void Contains_DayBeforeTheFirstDay_IsFalse()
    {
        DateRange range = DateRange.Create(First, Last);

        range.Contains(First.AddDays(-1)).Should().BeFalse();
    }

    [Fact]
    public void Contains_DayAfterTheLastDay_IsFalse()
    {
        DateRange range = DateRange.Create(First, Last);

        range.Contains(Last.AddDays(1)).Should().BeFalse();
    }

    [Fact]
    public void Contains_DayInside_IsTrue()
    {
        DateRange range = DateRange.Create(First, Last);

        range.Contains(new DateOnly(2026, 3, 15)).Should().BeTrue();
    }

    [Fact]
    public void Create_BothEndsOnTheSameDay_CoversThatOneDay()
    {
        DateRange range = DateRange.Create(First, First);

        range.Contains(First).Should().BeTrue();
        range.Contains(First.AddDays(1)).Should().BeFalse();
        range.Contains(First.AddDays(-1)).Should().BeFalse();
    }

    [Fact]
    public void Create_ValidDays_KeepsBothEnds()
    {
        DateRange range = DateRange.Create(First, Last);

        range.From.Should().Be(First);
        range.To.Should().Be(Last);
    }

    [Fact]
    public void Create_LastDayBeforeFirstDay_IsRefused()
    {
        Func<DateRange> act = () => DateRange.Create(Last, First);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TryCreate_LastDayBeforeFirstDay_ReturnsFalse()
    {
        bool created = DateRange.TryCreate(Last, First, out _);

        created.Should().BeFalse();
    }

    [Fact]
    public void TryCreate_ValidDays_ReturnsTheRange()
    {
        bool created = DateRange.TryCreate(First, Last, out DateRange range);

        created.Should().BeTrue();
        range.Should().Be(DateRange.Create(First, Last));
    }
}
