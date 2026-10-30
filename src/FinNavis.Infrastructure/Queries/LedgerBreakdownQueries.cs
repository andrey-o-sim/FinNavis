using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;
using FinNavis.Infrastructure.Persistence;
using Npgsql;
using NpgsqlTypes;

namespace FinNavis.Infrastructure.Queries;

public sealed class LedgerBreakdownQueries(FinNavisDbContext context)
{
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
