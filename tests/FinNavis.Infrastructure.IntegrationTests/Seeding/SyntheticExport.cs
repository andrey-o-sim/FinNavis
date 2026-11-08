using System.Globalization;
using FinNavis.Infrastructure.IntegrationTests.Fixtures;

namespace FinNavis.Infrastructure.IntegrationTests.Seeding;

// An export of any length in the shape of the old tracker's real one: tab-separated UTF-16, the same
// header row, "yyyy.MM.dd" dates, signed amounts with an empty currency, a category with its group,
// and the merchant in the note. Every merchant, category and amount is invented.
internal static class SyntheticExport
{
    public const int HistoryRowCount = 1500;

    public const string FileName = "test-export.csv";

    public const string IncomeCategoryName = "Test pay";

    // Four rows a day, the way a busy month of the real file reads.
    private const int RowsPerDay = 4;

    // Every twentieth row is income; the rest are spending.
    private const int IncomeEvery = 20;

    private const int RandomSeed = 1_500;

    private const string DateFormat = "yyyy.MM.dd";

    private const string IncomeGroup = "Test earnings";

    private const string IncomeMerchant = "Test Employer";

    // Declared before the public lists below, because their initialisers read it.
    private static readonly (string Category, string Group, string Merchant)[] Spends =
    [
        ("Test books", "Test leisure", "Pixel Test Books"),
        ("Test fuel", "Test car", "Test Fuel Stop"),
        ("Test groceries", "Test food", "Test Corner Grocer"),
        ("Test cafe", "Test food", "Test Bean Cafe"),
        ("Test pharmacy", "Test health", "Test Green Pharmacy"),
        ("Test taxi", "Test transport", "Test Quick Taxi"),
    ];

    // Accounts no seeded ledger holds, so a load of the export has to create them.
    public static readonly string[] NewAccountNames = ["Test export card (UAH)", "Test export cash (UAH)"];

    public static readonly string[] ExpenseCategoryNames = [.. Spends.Select(spend => spend.Category)];

    // Starts the day after the seeded ledger ends, so no row of the export can be taken for a
    // seeded movement already in the ledger and every row is recorded.
    public static DateOnly FirstDay => LedgerSeeder.LastDay.AddDays(1);

    public static byte[] Create(int rowCount, IReadOnlyList<string> accountNames)
    {
        Random random = new(RandomSeed);
        string[][] rows = [.. Enumerable.Range(0, rowCount).Select(index => Row(index, accountNames, random))];

        return SyntheticFile.Export(SyntheticFile.ExportHeader, rows);
    }

    private static string[] Row(int index, IReadOnlyList<string> accountNames, Random random)
    {
        string date = FirstDay.AddDays(index / RowsPerDay).ToString(DateFormat, CultureInfo.InvariantCulture);
        string account = accountNames[index % accountNames.Count];

        if (index % IncomeEvery == 0)
        {
            decimal pay = random.Next(100_000, 3_000_000) / 100m;

            return [date, Format(pay), string.Empty, account, IncomeCategoryName, IncomeGroup, IncomeMerchant];
        }

        (string category, string group, string merchant) = Spends[index % Spends.Length];
        decimal spent = random.Next(100, 200_000) / 100m;

        return [date, Format(-spent), string.Empty, account, category, group, merchant];
    }

    // The real file drops trailing zeros: "-468", "-412.15".
    private static string Format(decimal amount) => amount.ToString("0.##", CultureInfo.InvariantCulture);
}
