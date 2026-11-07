using FinNavis.Domain.Enums;

namespace FinNavis.Application.Import.GetImportRun;

public sealed record ReportedSetAsideRow(
    int RowNumber,
    SetAsideReason Reason,
    string? SourceCategory,
    string RawLine);
