using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;
using FinNavis.Infrastructure.Persistence;
using Npgsql;
using NpgsqlTypes;

namespace FinNavis.Infrastructure.Queries;

/// <summary>
/// The balance half of the ledger's read path: what every account holds, and whether anything
/// recorded still names an account or a category (sad.md §6 flows 5, 6 and 7).
/// </summary>
/// <remarks>
/// <b>Nothing is stored and nothing is cached</b> (ADR-0002). Every figure is summed from the
/// movements at read time, which is what makes an edit or a removal free — there is no stored
/// total to correct (AC-21, AC-22).
/// <para>
/// <b>No entity is loaded</b> (ADR-0004). PostgreSQL does the summing and only the result rows
/// travel back.
/// </para>
/// </remarks>
public sealed class LedgerBalanceQueries(FinNavisDbContext context)
{
    // The three-part sum of ADR-0001, one correlated subquery per part so each one is answered by
    // its own index: idx_transactions_account_id, idx_transfers_source_account_id and
    // idx_transfers_destination_account_id. A transaction joins its category because the amount is
    // always positive and the direction comes from the category's side (data-model.md decision 3).
    private const string BalancesSql = """
        SELECT a.id,
               a.name,
               COALESCE((SELECT SUM(CASE WHEN c.kind = @expense_kind THEN -t.amount ELSE t.amount END)
                         FROM transactions t
                         JOIN categories c ON c.id = t.category_id
                         WHERE t.account_id = a.id), 0)
             - COALESCE((SELECT SUM(sent.amount)
                         FROM transfers sent
                         WHERE sent.source_account_id = a.id), 0)
             + COALESCE((SELECT SUM(received.amount)
                         FROM transfers received
                         WHERE received.destination_account_id = a.id), 0) AS balance
        FROM accounts a
        WHERE @account_id IS NULL OR a.id = @account_id
        ORDER BY a.name
        """;

    // A transfer names an account from either side, so all three are asked (AC-09). EXISTS stops at
    // the first row, so none of this aggregates.
    private const string AnyMovementNamesAccountSql = """
        SELECT EXISTS (SELECT 1 FROM transactions WHERE account_id = @account_id)
            OR EXISTS (SELECT 1 FROM transfers WHERE source_account_id = @account_id)
            OR EXISTS (SELECT 1 FROM transfers WHERE destination_account_id = @account_id)
        """;

    // Only transactions is read: a transfer carries no category, so it can never name one (AC-12).
    private const string AnyTransactionNamesCategorySql = """
        SELECT EXISTS (SELECT 1 FROM transactions WHERE category_id = @category_id)
        """;

    public Task<IReadOnlyList<AccountBalanceRow>> GetAccountBalancesAsync(
        Guid? accountId,
        CancellationToken cancellationToken) =>
        LedgerQuery.ReadAsync(
            context,
            BalancesSql,
            parameters =>
            {
                parameters.Add(NullableAccountId(accountId));
                parameters.AddWithValue("expense_kind", CategoryKind.Expense.ToString());
            },
            ReadBalanceRowsAsync,
            cancellationToken);

    public Task<bool> AnyMovementNamesAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken) =>
        LedgerQuery.ReadAsync(
            context,
            AnyMovementNamesAccountSql,
            parameters => parameters.AddWithValue("account_id", accountId),
            ReadFlagAsync,
            cancellationToken);

    public Task<bool> AnyTransactionNamesCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken) =>
        LedgerQuery.ReadAsync(
            context,
            AnyTransactionNamesCategorySql,
            parameters => parameters.AddWithValue("category_id", categoryId),
            ReadFlagAsync,
            cancellationToken);

    // Typed even when the value is absent, so "@account_id IS NULL" needs no cast in the SQL.
    private static NpgsqlParameter NullableAccountId(Guid? accountId) =>
        new("account_id", NpgsqlDbType.Uuid) { Value = (object?)accountId ?? DBNull.Value };

    private static async Task<IReadOnlyList<AccountBalanceRow>> ReadBalanceRowsAsync(
        NpgsqlDataReader reader,
        CancellationToken cancellationToken)
    {
        List<AccountBalanceRow> rows = [];

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new AccountBalanceRow(
                reader.GetGuid(0),
                reader.GetString(1),
                new Money(reader.GetDecimal(2))));
        }

        return rows;
    }

    private static async Task<bool> ReadFlagAsync(
        NpgsqlDataReader reader,
        CancellationToken cancellationToken) =>
        await reader.ReadAsync(cancellationToken) && reader.GetBoolean(0);
}
