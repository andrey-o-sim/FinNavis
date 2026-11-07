using System.Globalization;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Application.Import.Pipeline;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Import.MigrateHistory;

// The export names accounts and categories in the owner's own words, so what the ledger lacks is
// created rather than refused, and each creation is kept so undo can remove it later.
internal sealed class MigrationPolicy(
    StatementHeader header,
    IReadOnlyList<Account> existingAccounts,
    IReadOnlyList<Category> existingCategories,
    IAccountRepository accounts,
    ICategoryRepository categories,
    DateTimeOffset createdAt) : IImportPolicy
{
    private const string DateFormat = "yyyy.MM.dd";

    // The old tracker writes one opening balance per account as a row. It is not a movement.
    private const string AccountBalanceCategory = "Account balance";

    private readonly List<Account> _knownAccounts = [.. existingAccounts];
    private readonly List<Category> _knownCategories = [.. existingCategories];
    private readonly List<Account> _createdAccounts = [];
    private readonly List<Category> _createdCategories = [];

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

        Account account = FindOrCreateAccount(ValueOf(row, StatementColumn.Account));
        Category category = FindOrCreateCategory(categoryName, kind);

        return new RecordAsTransaction(
            account.Id, category.Id, new Money(Math.Abs(signedAmount)), DayOf(row), Note: null);
    }

    public void LinkCreationsTo(ImportRun run)
    {
        foreach (Account account in _createdAccounts)
        {
            run.LinkCreatedAccount(Guid.CreateVersion7(), account);
        }

        foreach (Category category in _createdCategories)
        {
            run.LinkCreatedCategory(Guid.CreateVersion7(), category);
        }
    }

    private Account FindOrCreateAccount(string name)
    {
        Account? known = _knownAccounts.Find(account => string.Equals(account.Name, name, StringComparison.Ordinal));

        if (known is not null)
        {
            return known;
        }

        Account created = Account.Create(Guid.CreateVersion7(), name, createdAt);
        accounts.Add(created);
        _knownAccounts.Add(created);
        _createdAccounts.Add(created);

        return created;
    }

    // A category is the name and the side together, so a refund in a spending category lands on an
    // earning category of the same name.
    private Category FindOrCreateCategory(string name, CategoryKind kind)
    {
        Category? known = _knownCategories.Find(category =>
            category.Kind == kind && string.Equals(category.Name, name, StringComparison.Ordinal));

        if (known is not null)
        {
            return known;
        }

        Category created = Category.Create(Guid.CreateVersion7(), name, kind, group: null, createdAt);
        categories.Add(created);
        _knownCategories.Add(created);
        _createdCategories.Add(created);

        return created;
    }

    private DateOnly DayOf(StatementRow row) =>
        DateOnly.ParseExact(ValueOf(row, StatementColumn.Date), DateFormat, CultureInfo.InvariantCulture);

    private string ValueOf(StatementRow row, StatementColumn column) =>
        row.Values[header.ColumnPositions[column]];
}
