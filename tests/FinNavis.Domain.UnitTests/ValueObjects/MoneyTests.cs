using FinNavis.Domain.ValueObjects;

namespace FinNavis.Domain.UnitTests.ValueObjects;

public sealed class MoneyTests
{
    [Fact]
    public void Equals_SameAmount_AreEqual()
    {
        var left = new Money(42.50m);
        var right = new Money(42.50m);

        left.Should().Be(right);
        (left == right).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentAmount_AreNotEqual()
    {
        var left = new Money(42.50m);
        var right = new Money(42.51m);

        left.Should().NotBe(right);
        (left != right).Should().BeTrue();
    }

    [Fact]
    public void Equals_SameAmountWithTrailingZeros_AreEqual()
    {
        // NUMERIC(19,4) reads 1.5 back as 1.5000. The two must stay one value.
        var written = new Money(1.5m);
        var read = new Money(1.5000m);

        written.Should().Be(read);
        written.GetHashCode().Should().Be(read.GetHashCode());
    }

    [Fact]
    public void Add_TwoAmounts_SumsThem()
    {
        Money sum = new Money(10.25m) + new Money(4.75m);

        sum.Should().Be(new Money(15.00m));
    }

    [Fact]
    public void Subtract_TwoAmounts_TakesOneFromTheOther()
    {
        Money rest = new Money(10.25m) - new Money(4.75m);

        rest.Should().Be(new Money(5.50m));
    }

    [Fact]
    public void Subtract_LargerAmount_GoesNegative()
    {
        // A balance is signed, so Money does not refuse a negative value.
        Money rest = new Money(4.75m) - new Money(10.25m);

        rest.Should().Be(new Money(-5.50m));
    }

    [Fact]
    public void Negate_Amount_FlipsTheSign()
    {
        Money negated = -new Money(7.00m);

        negated.Should().Be(new Money(-7.00m));
    }

    [Fact]
    public void Add_KeepsTheFourthDecimalPlace()
    {
        Money sum = new Money(0.0001m) + new Money(0.0002m);

        sum.Amount.Should().Be(0.0003m);
    }

    [Fact]
    public void Zero_IsAnAmountOfZero()
    {
        Money.Zero.Should().Be(new Money(0m));
    }

    [Fact]
    public void Zero_AddedToAnAmount_ChangesNothing()
    {
        Money sum = new Money(12.34m) + Money.Zero;

        sum.Should().Be(new Money(12.34m));
    }

    [Fact]
    public void IsPositive_AmountAboveZero_IsTrue()
    {
        new Money(0.0001m).IsPositive.Should().BeTrue();
    }

    [Fact]
    public void IsPositive_Zero_IsFalse()
    {
        Money.Zero.IsPositive.Should().BeFalse();
    }

    [Fact]
    public void IsPositive_NegativeAmount_IsFalse()
    {
        new Money(-0.0001m).IsPositive.Should().BeFalse();
    }

    // Without this the column rounds the figure on the way in while the entity still holds the
    // one it was handed, so the answer to a write and the next read disagree.
    [Fact]
    public void Amount_WithMoreDecimalsThanTheColumnHolds_IsRoundedToFour()
    {
        new Money(10.00004m).Amount.Should().Be(10.0000m);
        new Money(10.00006m).Amount.Should().Be(10.0001m);
    }

    // The rule PostgreSQL applies, so both sides land on the same value.
    [Fact]
    public void Amount_HalfwayBetweenTwoFourthDecimals_RoundsAwayFromZero()
    {
        new Money(0.00005m).Amount.Should().Be(0.0001m);
        new Money(-0.00005m).Amount.Should().Be(-0.0001m);
    }

    [Fact]
    public void Amount_RoundedDownToZero_IsNotPositive()
    {
        new Money(0.00004m).IsPositive.Should().BeFalse();
    }

    [Fact]
    public void FitsLedgerColumn_TheLargestFigureTheColumnHolds_IsTrue()
    {
        new Money(Money.Largest).FitsLedgerColumn.Should().BeTrue();
        new Money(-Money.Largest).FitsLedgerColumn.Should().BeTrue();
    }

    [Fact]
    public void FitsLedgerColumn_AFigureAboveTheColumn_IsFalse()
    {
        new Money(Money.Largest + 0.0001m).FitsLedgerColumn.Should().BeFalse();
        new Money(-Money.Largest - 0.0001m).FitsLedgerColumn.Should().BeFalse();
    }
}
