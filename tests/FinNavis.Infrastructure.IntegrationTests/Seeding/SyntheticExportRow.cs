using System.Globalization;

namespace FinNavis.Infrastructure.IntegrationTests.Seeding;

internal sealed record SyntheticExportRow(
    DateOnly Day,
    decimal SignedAmount,
    string Account,
    string Category,
    string CategoryGroup,
    string Merchant)
{
    private const string DateFormat = "yyyy.MM.dd";

    // The currency column is empty in the real file, so it is empty here too.
    public string[] ToFields() =>
    [
        Day.ToString(DateFormat, CultureInfo.InvariantCulture),
        Format(SignedAmount),
        string.Empty,
        Account,
        Category,
        CategoryGroup,
        Merchant,
    ];

    // The real file drops trailing zeros: "-468", "-412.15".
    private static string Format(decimal amount) => amount.ToString("0.##", CultureInfo.InvariantCulture);
}
