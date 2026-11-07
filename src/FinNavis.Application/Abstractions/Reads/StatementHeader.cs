namespace FinNavis.Application.Abstractions.Reads;

// The reader names every one of the three columns it could not find and does not decide which of
// them matter: a statement covers one account and names none, an export must name one per row, and
// that difference is the pipeline's policy, not the reader's.
public sealed record StatementHeader(
    IReadOnlyList<string> ColumnNames,
    string ShapeKey,
    IReadOnlySet<StatementColumn> MissingColumns,
    IReadOnlyDictionary<StatementColumn, int> ColumnPositions);
