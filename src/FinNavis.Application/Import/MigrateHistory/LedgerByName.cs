using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;

namespace FinNavis.Application.Import.MigrateHistory;

// The export names accounts and categories in the owner's own words, so what the ledger lacks is
// created rather than refused, and each creation is kept so undo can remove it later.
internal sealed class LedgerByName(
    IReadOnlyList<Account> existingAccounts,
    IReadOnlyList<Category> existingCategories,
    IAccountRepository accounts,
    ICategoryRepository categories,
    DateTimeOffset createdAt)
{
    private readonly List<Account> _knownAccounts = [.. existingAccounts];
    private readonly List<Category> _knownCategories = [.. existingCategories];
    private readonly List<Account> _createdAccounts = [];
    private readonly List<Category> _createdCategories = [];

    public Account FindOrCreateAccount(string name)
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
    public Category FindOrCreateCategory(string name, CategoryKind kind)
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
}
