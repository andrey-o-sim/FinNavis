using FinNavis.Domain.Enums;

namespace FinNavis.Application.Import.GetImportRun;

public sealed record ImportRunReport(
    Guid ImportRunId,
    ImportRunKind Kind,
    ImportRunStatus Status,
    string FileName,
    Guid? AccountId,
    int RecordedRowCount,
    int AlreadyPresentRowCount,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ReportedSetAsideRow> SetAside);
