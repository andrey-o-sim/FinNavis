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

    public static byte[] Create(int rowCount, IReadOnlyList<string> accountNames) =>
        ToFile(Rows(rowCount, accountNames, index => FirstDay.AddDays(index / RowsPerDay)));

    // The same three years the seeded ledger covers, so a load reads the seeded movements of the
    // accounts it names into its candidate snapshot instead of an empty stretch of the ledger.
    public static IReadOnlyList<SyntheticExportRow> RowsAcrossSeededYears(
        int rowCount,
        IReadOnlyList<string> accountNames)
    {
        int seededDays = LedgerSeeder.LastDay.DayNumber - LedgerSeeder.FirstDay.DayNumber + 1;

        return Rows(rowCount, accountNames, index => LedgerSeeder.FirstDay.AddDays(index * seededDays / rowCount));
    }

    public static byte[] ToFile(IEnumerable<SyntheticExportRow> rows) =>
        SyntheticFile.Export(SyntheticFile.ExportHeader, [.. rows.Select(row => row.ToFields())]);

    private static SyntheticExportRow[] Rows(
        int rowCount,
        IReadOnlyList<string> accountNames,
        Func<int, DateOnly> dayOf)
    {
        Random random = new(RandomSeed);

        return [.. Enumerable.Range(0, rowCount).Select(index => Row(index, dayOf(index), accountNames, random))];
    }

    private static SyntheticExportRow Row(int index, DateOnly day, IReadOnlyList<string> accountNames, Random random)
    {
        string account = accountNames[index % accountNames.Count];

        if (index % IncomeEvery == 0)
        {
            decimal pay = random.Next(100_000, 3_000_000) / 100m;

            return new SyntheticExportRow(day, pay, account, IncomeCategoryName, IncomeGroup, IncomeMerchant);
        }

        (string category, string group, string merchant) = Spends[index % Spends.Length];
        decimal spent = random.Next(100, 200_000) / 100m;

        return new SyntheticExportRow(day, -spent, account, category, group, merchant);
    }
}
