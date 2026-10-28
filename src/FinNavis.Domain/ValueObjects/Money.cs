namespace FinNavis.Domain.ValueObjects;

/// <summary>
/// An amount of money in the single currency the ledger tracks.
/// </summary>
/// <remarks>
/// No currency code is stored. The owner keeps one currency, so a code would be the same value on
/// every row (CLAUDE.md, PRD §3).
/// <para>
/// The amount is a <c>decimal</c> and never a <c>double</c> or a <c>float</c>, so the fourth
/// decimal place that PostgreSQL's <c>NUMERIC(19,4)</c> keeps survives the round trip.
/// </para>
/// <para>
/// A value may be negative, because a balance is signed. "An amount must be above zero" is AC-02,
/// and the entity that takes the amount refuses it — this type does not.
/// </para>
/// </remarks>
public readonly record struct Money(decimal Amount)
{
    /// <summary>Nothing. The starting point of a sum.</summary>
    public static Money Zero => new(0m);

    /// <summary>True when the amount is above zero. AC-02 is stated in terms of this.</summary>
    public bool IsPositive => Amount > 0m;

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);

    public static Money operator -(Money left, Money right) => new(left.Amount - right.Amount);

    public static Money operator -(Money value) => new(-value.Amount);
}
