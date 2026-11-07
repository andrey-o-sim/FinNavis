using System.Globalization;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Application.Import.Pipeline;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Import.LoadStatement;

// A statement covers the one account the owner named, so no column of the file is read to find an
// account: rows of several cards share one file and all of them land on that account.
internal sealed class StatementPolicy(
    Guid accountId,
    StatementHeader header,
    MappingProfile profile,
    string ledgerCurrency) : IImportPolicy
{
    private const string DateFormat = "dd.MM.yyyy HH:mm:ss";

    public ImportFileKind FileKind => ImportFileKind.Statement;

    public ImportRunKind RunKind => ImportRunKind.Statement;

    public Guid? RunAccountId => accountId;

    public int MatchWindowDays => 1;

    public RowDecision Decide(StatementRow row)
    {
        string sourceCategory = ValueOf(row, StatementColumn.Category);
        MappingRule? rule = profile.RuleCovering(sourceCategory);

        if (rule is null)
        {
            return new SetRowAside(SetAsideReason.NoRuleCovers, sourceCategory);
        }

        if (rule.Target == MappingRuleTarget.TransferBetweenOwnAccounts || rule.TargetCategoryId is not { } categoryId)
        {
            return new SetRowAside(SetAsideReason.TransferBetweenOwnAccounts, sourceCategory);
        }

        if (!string.Equals(ValueOf(row, StatementColumn.Currency), ledgerCurrency, StringComparison.Ordinal))
        {
            return new SetRowAside(SetAsideReason.ForeignCurrency, sourceCategory);
        }

        return new RecordAsTransaction(accountId, categoryId, PurchaseAmountOf(row), DayOf(row), Note: null);
    }

    private Money PurchaseAmountOf(StatementRow row) =>
        new(decimal.Parse(ValueOf(row, StatementColumn.Amount), CultureInfo.InvariantCulture));

    // The bank writes the time of day in its own zone; only the day is kept, as it stands in the file.
    private DateOnly DayOf(StatementRow row) =>
        DateOnly.FromDateTime(
            DateTime.ParseExact(ValueOf(row, StatementColumn.Date), DateFormat, CultureInfo.InvariantCulture));

    private string ValueOf(StatementRow row, StatementColumn column) =>
        row.Values[header.ColumnPositions[column]];
}
