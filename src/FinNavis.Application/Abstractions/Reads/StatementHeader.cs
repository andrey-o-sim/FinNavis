namespace FinNavis.Application.Abstractions.Reads;

public sealed record StatementHeader(IReadOnlyList<string> ColumnNames, string ShapeKey);
