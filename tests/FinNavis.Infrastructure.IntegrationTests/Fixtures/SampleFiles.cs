namespace FinNavis.Infrastructure.IntegrationTests.Fixtures;

// Synthetic files that copy the shape of a real statement and a real export: the header row, the
// column order, the date and amount formats. Every merchant and every amount is invented. A real
// file is a month of the owner's spending with the shop names attached, and it never enters the
// repository.
internal static class SampleFiles
{
    public const int ExportRowCount = 1500;

    public static string StatementPath { get; } = InFixtures("test-statement.csv");

    public static string ExportPath { get; } = InFixtures("test-export.csv");

    private static string InFixtures(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
    }
}
