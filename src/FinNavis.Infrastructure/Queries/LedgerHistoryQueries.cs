using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;
using FinNavis.Infrastructure.Persistence;
using Npgsql;
using NpgsqlTypes;

namespace FinNavis.Infrastructure.Queries;

/// <summary>
/// The history half of the ledger's read path: one page of recorded movements, transactions and
/// transfers together, newest first (AC-16, AC-17, sad.md §6 flow 8).
/// </summary>
/// <remarks>
/// <b>One <c>UNION ALL</c> over two tables</b> (ADR-0001), paged by keyset over
/// <c>(occurred_on, id)</c> — <c>id</c> is a time-ordered UUID v7 (ADR-0005), which is what makes
/// it a stable tiebreaker inside one day across both tables.
/// <para>
/// <b>No entity is loaded</b> (ADR-0004). The rows that travel back are read rows: a transaction
/// row carries its category and its note, a transfer row carries its two accounts, and neither can
/// carry the other's fields.
/// </para>
/// <para>
/// <b>A category filter drops the transfers branch of the union entirely</b> (settled at the
/// api-forge gate). A transfer carries no category, so a page that included one would not all
/// match what was asked.
/// </para>
/// </remarks>
public sealed class LedgerHistoryQueries(FinNavisDbContext context)
{
    private const string TransactionKind = "transaction";

    private const string TransactionsBranch = """
        SELECT 'transaction'::text AS kind,
               t.id                AS id,
               t.occurred_on       AS occurred_on,
               t.amount            AS amount,
               t.created_at        AS created_at,
               t.account_id        AS account_id,
               a.name              AS account_name,
               t.category_id       AS category_id,
               c.name              AS category_name,
               c.kind              AS category_kind,
               t.note              AS note,
               NULL::uuid          AS source_account_id,
               NULL::text          AS source_account_name,
               NULL::uuid          AS destination_account_id,
               NULL::text          AS destination_account_name
        FROM transactions t
        JOIN accounts a ON a.id = t.account_id
        JOIN categories c ON c.id = t.category_id
        """;

    private const string TransfersBranch = """
        SELECT 'transfer'::text AS kind,
               f.id,
               f.occurred_on,
               f.amount,
               f.created_at,
               NULL::uuid AS account_id,
               NULL::text AS account_name,
               NULL::uuid AS category_id,
               NULL::text AS category_name,
               NULL::text AS category_kind,
               NULL::text AS note,
               f.source_account_id,
               s.name,
               f.destination_account_id,
               d.name
        FROM transfers f
        JOIN accounts s ON s.id = f.source_account_id
        JOIN accounts d ON d.id = f.destination_account_id
        """;

    public async Task<HistoryPage> GetHistoryPageAsync(
        HistoryFilter filter,
        HistoryPageRequest page,
        CancellationToken cancellationToken)
    {
        int limit = RequireLimitWithinBounds(page.Limit);
        (HistoryCursor? boundary, bool readingForward) = ReadDirection(page);

        // One row over the limit, so the page can tell "full" from "there is more" without a
        // second count over both tables.
        IReadOnlyList<IHistoryRow> fetched = await LedgerQuery.ReadAsync(
            context,
            BuildSql(filter, boundary, readingForward),
            parameters => Bind(parameters, filter, boundary, limit + 1),
            ReadRowsAsync,
            cancellationToken);

        bool hasMoreBeyondThePage = fetched.Count > limit;
        List<IHistoryRow> items = [.. fetched.Take(limit)];

        // The backward read walks away from the boundary, so it comes back oldest first.
        if (!readingForward)
        {
            items.Reverse();
        }

        // Reading backwards means a later page is where the caller came from, so one exists; and
        // reading forwards from a cursor means an earlier page is where the cursor came from.
        bool hasNext = !readingForward || hasMoreBeyondThePage;
        bool hasPrev = readingForward ? boundary is not null : hasMoreBeyondThePage;

        return new HistoryPage(
            items,
            hasNext,
            hasPrev,
            NextCursor: hasNext && items.Count > 0 ? CursorOf(items[^1]).Encode() : null,
            PrevCursor: hasPrev && items.Count > 0 ? CursorOf(items[0]).Encode() : null);
    }

    private static int RequireLimitWithinBounds(int limit)
    {
        if (limit is < HistoryPageRequest.MinLimit or > HistoryPageRequest.MaxLimit)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                limit,
                $"A history page holds between {HistoryPageRequest.MinLimit} and {HistoryPageRequest.MaxLimit} movements.");
        }

        return limit;
    }

    private static (HistoryCursor? Boundary, bool ReadingForward) ReadDirection(HistoryPageRequest page)
    {
        bool hasAfter = !string.IsNullOrWhiteSpace(page.After);
        bool hasBefore = !string.IsNullOrWhiteSpace(page.Before);

        if (hasAfter && hasBefore)
        {
            throw new ArgumentException(
                "A page is read in one direction: pass after or before, not both.",
                nameof(page));
        }

        if (!hasAfter && !hasBefore)
        {
            return (null, true);
        }

        string token = (hasAfter ? page.After : page.Before)!;

        if (!HistoryCursor.TryDecode(token, out HistoryCursor cursor))
        {
            throw new ArgumentException(
                "That is not a cursor this server handed out.",
                nameof(page));
        }

        return (cursor, hasAfter);
    }

    private static string BuildSql(HistoryFilter filter, HistoryCursor? boundary, bool readingForward)
    {
        string order = readingForward ? "DESC" : "ASC";

        List<string> branches = [Branch(TransactionsBranch, TransactionConditions(filter, boundary, readingForward))];

        if (filter.CategoryId is null)
        {
            branches.Add(Branch(TransfersBranch, TransferConditions(filter, boundary, readingForward)));
        }

        return $"""
            {string.Join("\nUNION ALL\n", branches)}
            ORDER BY occurred_on {order}, id {order}
            LIMIT @limit
            """;
    }

    private static string Branch(string select, IReadOnlyList<string> conditions) =>
        conditions.Count == 0 ? select : $"{select}\nWHERE {string.Join(" AND ", conditions)}";

    private static List<string> TransactionConditions(
        HistoryFilter filter,
        HistoryCursor? boundary,
        bool readingForward)
    {
        List<string> conditions = [];

        if (filter.From is not null)
        {
            conditions.Add("t.occurred_on >= @from");
        }

        if (filter.To is not null)
        {
            conditions.Add("t.occurred_on <= @to");
        }

        if (filter.AccountId is not null)
        {
            conditions.Add("t.account_id = @account_id");
        }

        if (filter.CategoryId is not null)
        {
            conditions.Add("t.category_id = @category_id");
        }

        if (boundary is not null)
        {
            conditions.Add($"(t.occurred_on, t.id) {Comparison(readingForward)} (@cursor_day, @cursor_id)");
        }

        return conditions;
    }

    private static List<string> TransferConditions(
        HistoryFilter filter,
        HistoryCursor? boundary,
        bool readingForward)
    {
        List<string> conditions = [];

        if (filter.From is not null)
        {
            conditions.Add("f.occurred_on >= @from");
        }

        if (filter.To is not null)
        {
            conditions.Add("f.occurred_on <= @to");
        }

        // A transfer touches the account from either side, so either side keeps the row (AC-16).
        if (filter.AccountId is not null)
        {
            conditions.Add("(f.source_account_id = @account_id OR f.destination_account_id = @account_id)");
        }

        if (boundary is not null)
        {
            conditions.Add($"(f.occurred_on, f.id) {Comparison(readingForward)} (@cursor_day, @cursor_id)");
        }

        return conditions;
    }

    // Newest first, so reading on means moving to smaller (occurred_on, id) pairs.
    private static string Comparison(bool readingForward) => readingForward ? "<" : ">";

    private static void Bind(
        NpgsqlParameterCollection parameters,
        HistoryFilter filter,
        HistoryCursor? boundary,
        int rowsToFetch)
    {
        parameters.AddWithValue("limit", rowsToFetch);

        if (filter.From is DateOnly from)
        {
            parameters.AddWithValue("from", NpgsqlDbType.Date, from);
        }

        if (filter.To is DateOnly to)
        {
            parameters.AddWithValue("to", NpgsqlDbType.Date, to);
        }

        if (filter.AccountId is Guid accountId)
        {
            parameters.AddWithValue("account_id", accountId);
        }

        if (filter.CategoryId is Guid categoryId)
        {
            parameters.AddWithValue("category_id", categoryId);
        }

        if (boundary is HistoryCursor cursor)
        {
            parameters.AddWithValue("cursor_day", NpgsqlDbType.Date, cursor.OccurredOn);
            parameters.AddWithValue("cursor_id", cursor.Id);
        }
    }

    private static async Task<IReadOnlyList<IHistoryRow>> ReadRowsAsync(
        NpgsqlDataReader reader,
        CancellationToken cancellationToken)
    {
        List<IHistoryRow> rows = [];

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(reader.GetString(0) == TransactionKind ? ReadTransaction(reader) : ReadTransfer(reader));
        }

        return rows;
    }

    private static HistoryTransactionRow ReadTransaction(NpgsqlDataReader reader) =>
        new(
            reader.GetGuid(1),
            reader.GetFieldValue<DateOnly>(2),
            new Money(reader.GetDecimal(3)),
            reader.GetGuid(5),
            reader.GetString(6),
            reader.GetGuid(7),
            reader.GetString(8),
            Enum.Parse<CategoryKind>(reader.GetString(9)),
            reader.IsDBNull(10) ? null : reader.GetString(10),
            reader.GetFieldValue<DateTimeOffset>(4));

    private static HistoryTransferRow ReadTransfer(NpgsqlDataReader reader) =>
        new(
            reader.GetGuid(1),
            reader.GetFieldValue<DateOnly>(2),
            new Money(reader.GetDecimal(3)),
            reader.GetGuid(11),
            reader.GetString(12),
            reader.GetGuid(13),
            reader.GetString(14),
            reader.GetFieldValue<DateTimeOffset>(4));

    private static HistoryCursor CursorOf(IHistoryRow row) => new(row.OccurredOn, row.Id);
}
