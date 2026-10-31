using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;

namespace FinNavis.Domain.Entities;

// There is no Failed status, no undone_at and no mapping profile id. A load that fails writes no
// run at all, nothing asks when a run was undone, and nothing reads which profile a load applied.
public sealed class ImportRun
{
    public const int MaxFileNameLength = 255;

    private readonly List<ImportTransactionLink> _transactionLinks = [];
    private readonly List<ImportTransferLink> _transferLinks = [];
    private readonly List<ImportCreatedAccountLink> _createdAccountLinks = [];
    private readonly List<ImportCreatedCategoryLink> _createdCategoryLinks = [];
    private readonly List<SetAsideRow> _setAsideRows = [];

    private ImportRun(
        Guid id,
        string fileName,
        ImportRunKind kind,
        ImportRunStatus status,
        Guid? accountId,
        int recordedRowCount,
        int alreadyPresentRowCount,
        DateTimeOffset createdAt)
    {
        Id = id;
        FileName = fileName;
        Kind = kind;
        Status = status;
        AccountId = accountId;
        RecordedRowCount = recordedRowCount;
        AlreadyPresentRowCount = alreadyPresentRowCount;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string FileName { get; private set; }

    public ImportRunKind Kind { get; private set; }

    public ImportRunStatus Status { get; private set; }

    public Guid? AccountId { get; private set; }

    // Stored rather than counted from the links: undo removes the links, and the report must
    // still say what the load had recorded.
    public int RecordedRowCount { get; private set; }

    public int AlreadyPresentRowCount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<ImportTransactionLink> TransactionLinks => _transactionLinks;

    public IReadOnlyList<ImportTransferLink> TransferLinks => _transferLinks;

    public IReadOnlyList<ImportCreatedAccountLink> CreatedAccountLinks => _createdAccountLinks;

    public IReadOnlyList<ImportCreatedCategoryLink> CreatedCategoryLinks => _createdCategoryLinks;

    public IReadOnlyList<SetAsideRow> SetAsideRows => _setAsideRows;

    // The whole run commits in one database transaction, so a run that exists has completed.
    public static ImportRun Record(
        Guid id,
        string fileName,
        ImportRunKind kind,
        Guid? accountId,
        DateTimeOffset createdAt)
    {
        RequireValidFileName(fileName);
        RequireAccountMatchingKind(kind, accountId);

        return new ImportRun(
            id,
            fileName,
            kind,
            ImportRunStatus.Completed,
            accountId,
            recordedRowCount: 0,
            alreadyPresentRowCount: 0,
            createdAt);
    }

    public void LinkRecordedTransaction(Guid linkId, Transaction transaction)
    {
        _transactionLinks.Add(ImportTransactionLink.For(linkId, Id, transaction, CreatedAt));
        RecordedRowCount++;
    }

    public void LinkRecordedTransfer(Guid linkId, Transfer transfer)
    {
        _transferLinks.Add(ImportTransferLink.For(linkId, Id, transfer, CreatedAt));
        RecordedRowCount++;
    }

    public void CountAlreadyPresentRow() => AlreadyPresentRowCount++;

    public void LinkCreatedAccount(Guid linkId, Account account) =>
        _createdAccountLinks.Add(ImportCreatedAccountLink.For(linkId, Id, account, CreatedAt));

    public void LinkCreatedCategory(Guid linkId, Category category) =>
        _createdCategoryLinks.Add(ImportCreatedCategoryLink.For(linkId, Id, category, CreatedAt));

    public void SetAside(
        Guid rowId,
        int rowNumber,
        SetAsideReason reason,
        string? sourceCategory,
        string rawLine) =>
        _setAsideRows.Add(
            SetAsideRow.For(rowId, Id, rowNumber, reason, sourceCategory, rawLine, CreatedAt));

    public void MarkUndone()
    {
        if (Status == ImportRunStatus.Undone)
        {
            throw new DomainRuleViolationException(
                "Import.AlreadyUndone",
                "This load was already undone, so there is nothing left to remove.");
        }

        Status = ImportRunStatus.Undone;
    }

    // The file name comes from the upload, which the use case checks first, so a value that
    // reaches this point out of bounds is a defect, not owner input.
    private static void RequireValidFileName(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            fileName.Length, MaxFileNameLength, nameof(fileName));
    }

    // A statement covers one account by definition. A history export names an account on every
    // row instead, so its run names none.
    private static void RequireAccountMatchingKind(ImportRunKind kind, Guid? accountId)
    {
        bool accountExpected = kind == ImportRunKind.Statement;

        if (accountExpected != accountId.HasValue)
        {
            throw new ArgumentException(
                accountExpected
                    ? "A statement run must name the account it was loaded onto."
                    : "A history migration run names no account of its own.",
                nameof(accountId));
        }
    }
}
