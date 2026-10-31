namespace FinNavis.Application.Abstractions.Reads;

public sealed record StatementRow(int LineNumber, IReadOnlyList<string> Values, string RawLine);
