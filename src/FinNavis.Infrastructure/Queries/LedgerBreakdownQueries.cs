using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;
using FinNavis.Infrastructure.Persistence;
using Npgsql;
using NpgsqlTypes;

namespace FinNavis.Infrastructure.Queries;

/// <summary>
/// The breakdown half of the ledger's read path: a period's totals, one row per category that
/// recorded something (AC-18, AC-19, AC-20, sad.md §6 flow 3).
/// </summary>
/// <remarks>
/// <b>One aggregate query, and it never reads <c>transfers</c></b> (ADR-0001). AC-20 holds because
/// the statement cannot reach that table, not because a filter removes its rows — see
/// <see cref="Sql"/>.
/// <para>
/// <b>The category-to-group mapping is resolved at query time</b> (ADR-0002), so moving a category
/// between groups changes the group figures at once and can never change that category's own
/// figure for a closed period (AC-13).
/// </para>
/// <para>
/// <b>Rows come back raw.</b> Totalling each group and assembling the Default bucket for the
/// categories with no group is the use case's job — the Default group is produced at read time and
/// is never a row (sad.md §12, <c>data-model.md</c> decision 8).
/// </para>
/// </remarks>
public sealed class LedgerBreakdownQueries(FinNavisDbContext context)
{
    /// <summary>
    /// The one statement this adapter runs.
    /// </summary>
    /// <remarks>
    /// Public so that AC-20 can be checked by reading the statement: a test asserts it names no
    /// <c>transfers</c> table, which is a stronger guarantee than asserting that no transfer turned
    /// up in one test's result.
    /// <para>
    /// The period is the leading predicate, which is what
    /// <c>idx_transactions_occurred_on_account_id</c> is there for; the optional account is that
    /// index's second column. The group link is joined on
    /// <c>categories.category_group_id</c>, covered by <c>idx_categories_category_group_id</c>.
    /// </para>
    /// <para>
    /// A <c>LEFT JOIN</c>, because a category with no group still has totals — it reaches the
    /// Default bucket by having no group at all. A group that recorded nothing in the period has
    /// no row here and is left out of the report rather than reported as zero (api-forge gate).
    /// </para>
    /// </remarks>
    public const string Sql = """
        SELECT c.id              AS category_id,
               c.name            AS category_name,
               c.kind            AS category_kind,
               c.category_group_id,
               g.name            AS category_group_name,
               SUM(t.amount)     AS total
        FROM transactions t
        JOIN categories c ON c.id = t.category_id
        LEFT JOIN category_groups g ON g.id = c.category_group_id
        WHERE t.occurred_on >= @from
          AND t.occurred_on <= @to
          AND (@account_id IS NULL OR t.account_id = @account_id)
        GROUP BY c.id, c.name, c.kind, c.category_group_id, g.name
        ORDER BY c.kind, c.name
        """;

    public Task<IReadOnlyList<BreakdownCategoryRow>> GetBreakdownAsync(
        DateRange period,
        Guid? accountId,
        CancellationToken cancellationToken) =>
        LedgerQuery.ReadAsync(
            context,
            Sql,
            parameters =>
            {
                parameters.AddWithValue("from", NpgsqlDbType.Date, period.From);
                parameters.AddWithValue("to", NpgsqlDbType.Date, period.To);
                parameters.Add(NullableAccountId(accountId));
            },
            ReadRowsAsync,
            cancellationToken);

    // Typed even when the value is absent, so "@account_id IS NULL" needs no cast in the SQL.
    private static NpgsqlParameter NullableAccountId(Guid? accountId) =>
        new("account_id", NpgsqlDbType.Uuid) { Value = (object?)accountId ?? DBNull.Value };

    private static async Task<IReadOnlyList<BreakdownCategoryRow>> ReadRowsAsync(
        NpgsqlDataReader reader,
        CancellationToken cancellationToken)
    {
        List<BreakdownCategoryRow> rows = [];

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new BreakdownCategoryRow(
                reader.GetGuid(0),
                reader.GetString(1),
                Enum.Parse<CategoryKind>(reader.GetString(2)),
                reader.IsDBNull(3) ? null : reader.GetGuid(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                new Money(reader.GetDecimal(5))));
        }

        return rows;
    }
}
