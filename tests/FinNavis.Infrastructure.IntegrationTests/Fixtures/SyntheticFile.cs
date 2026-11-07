using System.Text;

namespace FinNavis.Infrastructure.IntegrationTests.Fixtures;

// Builds small in-memory files in the two shapes the owner's real files have, so a test can change
// one line of a file without a new fixture on disk.
internal static class SyntheticFile
{
    public const string StatementTitlePadding = ",,,,,,,,,";

    public static readonly string[] StatementHeader =
    [
        "Дата", "Категорія", "Картка", "Опис операції", "Сума в валюті картки", "Валюта картки",
        "Сума в валюті транзакції", "Валюта транзакції", "Залишок на кінець періоду", "Валюта залишку",
    ];

    public const string StatementShapeKey =
        "дата|категорія|картка|опис операції|сума в валюті картки|валюта картки"
        + "|сума в валюті транзакції|валюта транзакції|залишок на кінець періоду|валюта залишку";

    public static readonly string[] ExportHeader =
        ["Date", "Amount", "Currency", "Account", "Category", "Category group", "Note"];

    public const string ExportShapeKey = "date|amount|currency|account|category|category group|note";

    private const string LineEnd = "\r\n";

    public static string StatementTitle(string period) => period + StatementTitlePadding;

    public static byte[] Statement(string titleLine, IEnumerable<string> header, params string[] rows)
    {
        string text = string.Join(LineEnd, [titleLine, string.Join(',', header), .. rows]) + LineEnd;

        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(text)];
    }

    public static byte[] Export(IEnumerable<string> header, params string[][] rows)
    {
        string text = string.Join(
            LineEnd,
            [string.Join('\t', header), .. rows.Select(row => string.Join('\t', row))]) + LineEnd;

        return [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes(text)];
    }
}
