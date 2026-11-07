using FinNavis.Application.Abstractions.Reads;

namespace FinNavis.Infrastructure.Import;

internal sealed record CsvFileLayout(
    bool OpensWithTitleLine,
    IReadOnlyDictionary<StatementColumn, string> RecognisedColumnNames,
    bool SkipsRowsWithNoDate)
{
    // A statement opens with a title line that names the period. It is skipped unread, because its
    // content changes every month and the bank pads it with commas so it looks like a row.
    // The statement records the purchase amount, which the bank writes unsigned, and takes its sign
    // from the card amount. A statement covers one account, so it has no account column.
    private static readonly CsvFileLayout Statement = new(
        OpensWithTitleLine: true,
        RecognisedColumnNames: new Dictionary<StatementColumn, string>
        {
            [StatementColumn.Date] = "дата",
            [StatementColumn.Category] = "категорія",
            [StatementColumn.AmountSign] = "сума в валюті картки",
            [StatementColumn.Amount] = "сума в валюті транзакції",
            [StatementColumn.Currency] = "валюта транзакції",
        },
        SkipsRowsWithNoDate: false);

    // The export ends with the tracker's own totals, which carry no date. They are not movements.
    private static readonly CsvFileLayout Export = new(
        OpensWithTitleLine: false,
        RecognisedColumnNames: new Dictionary<StatementColumn, string>
        {
            [StatementColumn.Date] = "date",
            [StatementColumn.Amount] = "amount",
            [StatementColumn.Currency] = "currency",
            [StatementColumn.Account] = "account",
            [StatementColumn.Category] = "category",
        },
        SkipsRowsWithNoDate: true);

    public static CsvFileLayout For(ImportFileKind kind)
    {
        return kind == ImportFileKind.Statement ? Statement : Export;
    }
}
