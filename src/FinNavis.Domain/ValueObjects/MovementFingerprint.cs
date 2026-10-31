using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FinNavis.Domain.ValueObjects;

// The import stores this hash and the undo recomputes it to see whether the owner edited the
// movement since. Every field is rendered culture-free, so a machine with a different culture
// computes the same hash and the two sides never disagree.
public readonly record struct MovementFingerprint
{
    public const int Length = 64;

    private const char FieldSeparator = '|';

    private MovementFingerprint(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static MovementFingerprint OfTransaction(
        Guid accountId,
        Guid categoryId,
        Money amount,
        DateOnly occurredOn,
        string? note) =>
        Hash(
            FormatId(accountId),
            FormatId(categoryId),
            FormatAmount(amount),
            FormatDate(occurredOn),
            note ?? string.Empty);

    public static MovementFingerprint OfTransfer(
        Guid sourceAccountId,
        Guid destinationAccountId,
        Money amount,
        DateOnly occurredOn) =>
        Hash(
            FormatId(sourceAccountId),
            FormatId(destinationAccountId),
            FormatAmount(amount),
            FormatDate(occurredOn));

    public override string ToString() => Value;

    private static MovementFingerprint Hash(params string[] fields)
    {
        byte[] joined = Encoding.UTF8.GetBytes(string.Join(FieldSeparator, fields));

        return new MovementFingerprint(Convert.ToHexStringLower(SHA256.HashData(joined)));
    }

    private static string FormatId(Guid id) => id.ToString("D", CultureInfo.InvariantCulture);

    private static string FormatAmount(Money amount) =>
        amount.Amount.ToString("F4", CultureInfo.InvariantCulture);

    private static string FormatDate(DateOnly date) =>
        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
