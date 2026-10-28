using FinNavis.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FinNavis.Infrastructure.Persistence.Converters;

/// <summary>
/// Maps <see cref="Money"/> to the single <c>NUMERIC(19,4)</c> column that holds it.
/// </summary>
/// <remarks>
/// The store side is <c>decimal</c> and never <c>double</c> or <c>float</c> (<c>CLAUDE.md</c>), so
/// the fourth decimal place survives the round trip.
/// <para>
/// No currency code crosses the converter. The owner keeps one currency, so a code would be the
/// same value on every row (PRD §3).
/// </para>
/// </remarks>
public sealed class MoneyConverter : ValueConverter<Money, decimal>
{
    public MoneyConverter()
        : base(money => money.Amount, amount => new Money(amount))
    {
    }
}
