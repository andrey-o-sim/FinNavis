namespace FinNavis.Domain.ValueObjects;

public readonly record struct Money(decimal Amount)
{
    public const int Scale = 4;

    // The ledger column is numeric(19,4): fifteen digits before the point and four after it.
    public const decimal Largest = 999_999_999_999_999.9999m;

    // Without this, PostgreSQL rounds the stored figure while the entity still holds the one it
    // was given, so the response to a write and the next read disagree. Away from zero is the
    // rule PostgreSQL applies, so both sides land on the same value.
    public decimal Amount { get; init; } = decimal.Round(Amount, Scale, MidpointRounding.AwayFromZero);

    public static Money Zero => new(0m);

    public bool IsPositive => Amount > 0m;

    public bool FitsLedgerColumn => Math.Abs(Amount) <= Largest;

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);

    public static Money operator -(Money left, Money right) => new(left.Amount - right.Amount);

    public static Money operator -(Money value) => new(-value.Amount);
}
