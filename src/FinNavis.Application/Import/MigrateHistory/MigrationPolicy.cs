using System.Globalization;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Application.Import.Pipeline;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Import.MigrateHistory;

internal sealed class MigrationPolicy(StatementHeader header, LedgerByName ledger) : IImportPolicy
{
    private const string DateFormat = "yyyy.MM.dd";

    // The old tracker writes one opening balance per account as a row. It is not a movement.
    private const string AccountBalanceCategory = "Account balance";

    public ImportFileKind FileKind => ImportFileKind.Export;

    public ImportRunKind RunKind => ImportRunKind.HistoryMigration;

    public Guid? RunAccountId => null;

    public int MatchWindowDays => 0;

    public RowDecision Decide(StatementRow row)
    {
        string categoryName = ValueOf(row, StatementColumn.Category);

        if (string.Equals(categoryName, AccountBalanceCategory, StringComparison.Ordinal))
        {
            return new IgnoreRow();
        }

        decimal signedAmount = decimal.Parse(ValueOf(row, StatementColumn.Amount), CultureInfo.InvariantCulture);
        CategoryKind kind = signedAmount < 0m ? CategoryKind.Expense : CategoryKind.Income;

        Account account = ledger.FindOrCreateAccount(ValueOf(row, StatementColumn.Account));
        Category category = ledger.FindOrCreateCategory(categoryName, kind);

        return new RecordAsTransaction(
            account.Id, category.Id, new Money(Math.Abs(signedAmount)), DayOf(row), Note: null);
    }

    public void LinkCreationsTo(ImportRun run) => ledger.LinkCreationsTo(run);

    private DateOnly DayOf(StatementRow row) =>
        DateOnly.ParseExact(ValueOf(row, StatementColumn.Date), DateFormat, CultureInfo.InvariantCulture);

    private string ValueOf(StatementRow row, StatementColumn column) =>
        row.Values[header.ColumnPositions[column]];
}
