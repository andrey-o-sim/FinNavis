using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;
using FinNavis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinNavis.Infrastructure.Queries;

public sealed class EfImportQueries(FinNavisDbContext context) : IImportQueries
{
    private const string TransactionKind = "transaction";

    private const string AccountKind = "account";

    public async Task<CandidateSnapshot> GetCandidateSnapshotAsync(
        IReadOnlyCollection<Guid> accountIds,
        DateRange period,
        CancellationToken cancellationToken)
    {
        Guid[] accounts = [.. accountIds];

        // Both tables in one statement, so the snapshot is a single picture of the ledger.
        List<CandidateRow> rows = await context.Database
            .SqlQuery<CandidateRow>($"""
                SELECT 'transaction'::text AS "Kind",
                       t.id                AS "Id",
                       t.occurred_on       AS "OccurredOn",
                       t.amount            AS "Amount",
                       t.account_id        AS "AccountId",
                       t.category_id       AS "CategoryId",
                       c.kind              AS "CategoryKind",
                       t.note              AS "Note",
                       t.external_ref      AS "ExternalRef",
                       NULL::uuid          AS "SourceAccountId",
                       NULL::uuid          AS "DestinationAccountId"
                FROM transactions t
                JOIN categories c ON c.id = t.category_id
                WHERE t.occurred_on BETWEEN {period.From} AND {period.To}
                  AND t.account_id = ANY({accounts})
                UNION ALL
                SELECT 'transfer'::text,
                       f.id,
                       f.occurred_on,
                       f.amount,
                       NULL::uuid,
                       NULL::uuid,
                       NULL::text,
                       NULL::text,
                       NULL::text,
                       f.source_account_id,
                       f.destination_account_id
                FROM transfers f
                WHERE f.occurred_on BETWEEN {period.From} AND {period.To}
                  AND (f.source_account_id = ANY({accounts}) OR f.destination_account_id = ANY({accounts}))
                """)
            .ToListAsync(cancellationToken);

        return new CandidateSnapshot(
            [.. rows.Where(row => row.Kind == TransactionKind).Select(ToTransaction)],
            [.. rows.Where(row => row.Kind != TransactionKind).Select(ToTransfer)]);
    }

    // A movement belongs to at most one run, so "outside this run" is a movement with no link to
    // this run — hand-typed, or recorded by another run.
    public async Task<ImportRunCreationsInUse> GetCreationsNamedOutsideRunAsync(
        Guid importRunId,
        CancellationToken cancellationToken)
    {
        List<CreationInUseRow> rows = await context.Database
            .SqlQuery<CreationInUseRow>($"""
                SELECT 'account'::text AS "Kind", l.account_id AS "Id"
                FROM import_created_account_links l
                WHERE l.import_run_id = {importRunId}
                  AND (EXISTS (SELECT 1 FROM transactions t
                               WHERE t.account_id = l.account_id
                                 AND NOT EXISTS (SELECT 1 FROM import_transaction_links x
                                                 WHERE x.transaction_id = t.id AND x.import_run_id = {importRunId}))
                    OR EXISTS (SELECT 1 FROM transfers f
                               WHERE f.source_account_id = l.account_id
                                 AND NOT EXISTS (SELECT 1 FROM import_transfer_links x
                                                 WHERE x.transfer_id = f.id AND x.import_run_id = {importRunId}))
                    OR EXISTS (SELECT 1 FROM transfers f
                               WHERE f.destination_account_id = l.account_id
                                 AND NOT EXISTS (SELECT 1 FROM import_transfer_links x
                                                 WHERE x.transfer_id = f.id AND x.import_run_id = {importRunId})))
                UNION ALL
                SELECT 'category'::text, l.category_id
                FROM import_created_category_links l
                WHERE l.import_run_id = {importRunId}
                  AND EXISTS (SELECT 1 FROM transactions t
                              WHERE t.category_id = l.category_id
                                AND NOT EXISTS (SELECT 1 FROM import_transaction_links x
                                                WHERE x.transaction_id = t.id AND x.import_run_id = {importRunId}))
                """)
            .ToListAsync(cancellationToken);

        return new ImportRunCreationsInUse(
            [.. rows.Where(row => row.Kind == AccountKind).Select(row => row.Id)],
            [.. rows.Where(row => row.Kind != AccountKind).Select(row => row.Id)]);
    }

    private static CandidateTransactionRow ToTransaction(CandidateRow row) =>
        new(
            row.Id,
            row.OccurredOn,
            new Money(row.Amount),
            row.AccountId!.Value,
            row.CategoryId!.Value,
            Enum.Parse<CategoryKind>(row.CategoryKind!),
            row.Note,
            row.ExternalRef);

    private static CandidateTransferRow ToTransfer(CandidateRow row) =>
        new(
            row.Id,
            row.OccurredOn,
            new Money(row.Amount),
            row.SourceAccountId!.Value,
            row.DestinationAccountId!.Value);

    private sealed record CandidateRow(
        string Kind,
        Guid Id,
        DateOnly OccurredOn,
        decimal Amount,
        Guid? AccountId,
        Guid? CategoryId,
        string? CategoryKind,
        string? Note,
        string? ExternalRef,
        Guid? SourceAccountId,
        Guid? DestinationAccountId);

    private sealed record CreationInUseRow(string Kind, Guid Id);
}
