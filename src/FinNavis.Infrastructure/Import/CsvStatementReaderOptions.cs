namespace FinNavis.Infrastructure.Import;

public sealed record CsvStatementReaderOptions(int? RowCeiling)
{
    public static CsvStatementReaderOptions WithoutCeiling { get; } = new(RowCeiling: null);
}
