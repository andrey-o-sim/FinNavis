using System.ComponentModel.DataAnnotations.Schema;
using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;
using FinNavis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace FinNavis.Infrastructure.Queries;

public sealed class EfImportQueries : IImportQueries
{
    private const string TransactionKind = "transaction";

    // One statement over both tables, so the two halves are one picture of the ledger taken at one
    // moment. A transfer is a candidate from either side, because a statement row for the account
    // can be the money leaving or the money arriving.
    private const string CandidateSnapshotSql = """
        SELECT 'transaction'::text AS kind,
               t.id                AS id,
               t.occurred_on       AS occurred_on,
               t.amount            AS amount,
               t.account_id        AS account_id,
               t.category_id       AS category_id,
               c.kind              AS category_kind,
               t.note              AS note,
               t.external_ref      AS external_ref,
               NULL::uuid          AS source_account_id,
               NULL::uuid          AS destination_account_id
        FROM transactions t
        JOIN categories c ON c.id = t.category_id
        WHERE t.occurred_on BETWEEN @from AND @to
          AND t.account_id = ANY(@account_ids)
        UNION ALL
        SELECT 'transfer'::text AS kind,
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
        WHERE f.occurred_on BETWEEN @from AND @to
          AND (f.source_account_id = ANY(@account_ids) OR f.destination_account_id = ANY(@account_ids))
        """;

    private readonly FinNavisDbContext _context;

    public EfImportQueries(FinNavisDbContext context) => _context = context;

    public async Task<CandidateSnapshot> GetCandidateSnapshotAsync(
        IReadOnlyCollection<Guid> accountIds,
        DateRange period,
        CancellationToken cancellationToken)
    {
        List<CandidateMovementRow> rows = await _context.Database
            .SqlQueryRaw<CandidateMovementRow>(
                CandidateSnapshotSql,
                new NpgsqlParameter("from", NpgsqlDbType.Date) { Value = period.From },
                new NpgsqlParameter("to", NpgsqlDbType.Date) { Value = period.To },
                new NpgsqlParameter("account_ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid)
                {
                    Value = accountIds.ToArray(),
                })
            .ToListAsync(cancellationToken);

        return new CandidateSnapshot(
            [.. rows.Where(IsTransaction).Select(ToTransaction)],
            [.. rows.Where(row => !IsTransaction(row)).Select(ToTransfer)]);
    }

    public Task<ImportRunCreationsInUse> GetCreationsNamedOutsideRunAsync(
        Guid importRunId,
        CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    private static bool IsTransaction(CandidateMovementRow row) => row.Kind == TransactionKind;

    private static CandidateTransactionRow ToTransaction(CandidateMovementRow row) =>
        new(
            row.Id,
            row.OccurredOn,
            new Money(row.Amount),
            row.AccountId!.Value,
            row.CategoryId!.Value,
            Enum.Parse<CategoryKind>(row.CategoryKind!),
            row.Note,
            row.ExternalRef);

    private static CandidateTransferRow ToTransfer(CandidateMovementRow row) =>
        new(
            row.Id,
            row.OccurredOn,
            new Money(row.Amount),
            row.SourceAccountId!.Value,
            row.DestinationAccountId!.Value);

    private sealed class CandidateMovementRow
    {
        [Column("kind")]
        public string Kind { get; init; } = string.Empty;

        [Column("id")]
        public Guid Id { get; init; }

        [Column("occurred_on")]
        public DateOnly OccurredOn { get; init; }

        [Column("amount")]
        public decimal Amount { get; init; }

        [Column("account_id")]
        public Guid? AccountId { get; init; }

        [Column("category_id")]
        public Guid? CategoryId { get; init; }

        [Column("category_kind")]
        public string? CategoryKind { get; init; }

        [Column("note")]
        public string? Note { get; init; }

        [Column("external_ref")]
        public string? ExternalRef { get; init; }

        [Column("source_account_id")]
        public Guid? SourceAccountId { get; init; }

        [Column("destination_account_id")]
        public Guid? DestinationAccountId { get; init; }
    }
}
