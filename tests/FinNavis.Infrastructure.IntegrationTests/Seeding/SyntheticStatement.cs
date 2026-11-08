using System.Globalization;
using FinNavis.Infrastructure.IntegrationTests.Fixtures;

namespace FinNavis.Infrastructure.IntegrationTests.Seeding;

// A bank statement of any length in the shape of the real one: the title line naming the period,
// the same header row, "dd.MM.yyyy HH:mm:ss" dates, the card amount signed and the purchase amount
// unsigned. Every merchant and amount is invented. No row is a refund.
internal static class SyntheticStatement
{
    public const string FileName = "test-statement.csv";

    public const string Card = "**** 0001";

    // Three rows a day, the way a busy month of the real file reads.
    private const int RowsPerDay = 3;

    private const int RandomSeed = 3_003;

    private const string DayFormat = "dd.MM.yyyy";

    private const decimal OpeningBalance = 50_000m;

    // Starts a year after the seeded ledger ends, clear of the export too, so no row of the
    // statement can be taken for a movement already in the ledger.
    public static DateOnly FirstDay => LedgerSeeder.LastDay.AddYears(1).AddDays(1);

    public static byte[] Create(int rowCount, IReadOnlyList<string> sourceCategories, string currency)
    {
        Random random = new(RandomSeed);
        decimal balance = OpeningBalance;
        List<string> rows = [];

        for (int index = 0; index < rowCount; index++)
        {
            decimal spent = random.Next(100, 200_000) / 100m;
            balance -= spent;
            rows.Add(Row(index, sourceCategories[index % sourceCategories.Count], spent, balance, currency));
        }

        DateOnly lastDay = FirstDay.AddDays((rowCount - 1) / RowsPerDay);

        return SyntheticFile.Statement(Title(lastDay), SyntheticFile.StatementHeader, [.. rows]);
    }

    private static string Title(DateOnly lastDay) =>
        SyntheticFile.StatementTitle(
            $"Виписка за період {FormatDay(FirstDay)} - {FormatDay(lastDay)}");

    private static string Row(int index, string sourceCategory, decimal spent, decimal balance, string currency)
    {
        DateOnly day = FirstDay.AddDays(index / RowsPerDay);
        string time = $"{10 + (index % RowsPerDay):00}:00:00";
        string amount = Format(spent);

        return string.Join(
            ',',
            $"{FormatDay(day)} {time}",
            sourceCategory,
            Card,
            $"Test Merchant {index % 40}",
            $"-{amount}",
            currency,
            amount,
            currency,
            Format(balance),
            currency);
    }

    private static string FormatDay(DateOnly day) => day.ToString(DayFormat, CultureInfo.InvariantCulture);

    // The real file drops trailing zeros: "-760", "-132.3".
    private static string Format(decimal amount) => amount.ToString("0.##", CultureInfo.InvariantCulture);
}
