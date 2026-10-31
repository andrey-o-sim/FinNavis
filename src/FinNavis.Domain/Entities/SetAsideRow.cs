using FinNavis.Domain.Enums;

namespace FinNavis.Domain.Entities;

// No status and no reference into the ledger: only a later load of the same file can record this
// row, and that load writes its own run.
public sealed class SetAsideRow
{
    public const int MaxSourceCategoryLength = 200;

    public const int MaxRawLineLength = 2000;

    private SetAsideRow(
        Guid id,
        Guid importRunId,
        int rowNumber,
        SetAsideReason reason,
        string? sourceCategory,
        string rawLine,
        DateTimeOffset createdAt)
    {
        Id = id;
        ImportRunId = importRunId;
        RowNumber = rowNumber;
        Reason = reason;
        SourceCategory = sourceCategory;
        RawLine = rawLine;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid ImportRunId { get; private set; }

    public int RowNumber { get; private set; }

    public SetAsideReason Reason { get; private set; }

    public string? SourceCategory { get; private set; }

    public string RawLine { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    // The reader refuses a file whose line or category is over these bounds before anything is
    // recorded, so a value over the bound that reaches this point is a defect, not owner input.
    internal static SetAsideRow For(
        Guid id,
        Guid importRunId,
        int rowNumber,
        SetAsideReason reason,
        string? sourceCategory,
        string rawLine,
        DateTimeOffset createdAt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rowNumber, 1);
        ArgumentNullException.ThrowIfNull(rawLine);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            rawLine.Length, MaxRawLineLength, nameof(rawLine));

        if (sourceCategory is not null)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(
                sourceCategory.Length, MaxSourceCategoryLength, nameof(sourceCategory));
        }

        return new SetAsideRow(
            id, importRunId, rowNumber, reason, sourceCategory, rawLine, createdAt);
    }
}
