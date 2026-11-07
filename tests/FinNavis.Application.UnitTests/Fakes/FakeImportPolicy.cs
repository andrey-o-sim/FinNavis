using System.Globalization;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Application.Import.Pipeline;
using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.UnitTests.Fakes;

// Reads a synthetic row laid out as date, amount, category, note, and files every row on the one
// account it was given. A category with no rule is set aside, which is what lets a test add a rule
// between two loads of the same file.
internal sealed class FakeImportPolicy(
    ImportRunKind runKind,
    Guid accountId,
    int matchWindowDays) : IImportPolicy
{
    private const int DateColumn = 0;
    private const int AmountColumn = 1;
    private const int CategoryColumn = 2;
    private const int NoteColumn = 3;

    private readonly Dictionary<string, Guid> _rules = new(StringComparer.Ordinal);

    public ImportFileKind FileKind =>
        runKind == ImportRunKind.Statement ? ImportFileKind.Statement : ImportFileKind.Export;

    public ImportRunKind RunKind => runKind;

    public Guid? RunAccountId => runKind == ImportRunKind.Statement ? accountId : null;

    public int MatchWindowDays => matchWindowDays;

    public FakeImportPolicy Cover(string sourceCategory, Guid categoryId)
    {
        _rules[sourceCategory] = categoryId;

        return this;
    }

    public RowDecision Decide(StatementRow row)
    {
        string sourceCategory = row.Values[CategoryColumn];

        if (!_rules.TryGetValue(sourceCategory, out Guid categoryId))
        {
            return new SetRowAside(SetAsideReason.NoRuleCovers, sourceCategory);
        }

        string? note = row.Values.Count > NoteColumn ? row.Values[NoteColumn] : null;

        return new RecordAsTransaction(
            accountId,
            categoryId,
            new Money(decimal.Parse(row.Values[AmountColumn], CultureInfo.InvariantCulture)),
            DateOnly.ParseExact(row.Values[DateColumn], "yyyy-MM-dd", CultureInfo.InvariantCulture),
            note);
    }
}
