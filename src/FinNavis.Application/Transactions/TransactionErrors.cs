using FinNavis.Domain.Errors;

namespace FinNavis.Application.Transactions;

/// <summary>
/// The refusals the transaction use cases raise before the entity is reached.
/// </summary>
/// <remarks>
/// Recording and editing both name an account and a category, so writing the two lookups'
/// refusals once is what keeps the wording identical on both paths. The wording itself is the PRD
/// acceptance criterion, not invented (sad.md §8).
/// <para>
/// <b>The account and the category refusals carry their own module</b>, not this one. The module
/// of a <c>*.NotFound</c> code names the aggregate that was not found, never the aggregate the
/// request was operating on (<c>docs/adr/0004</c>), so a transaction naming an unknown category
/// refuses with the same <c>Category.NotFound</c> that <c>renameCategory</c> returns.
/// </para>
/// </remarks>
internal static class TransactionErrors
{
    public static DomainRuleViolationException NotFound(Guid transactionId) =>
        new("Transaction.NotFound", $"No transaction in the ledger has id {transactionId}.");

    public static DomainRuleViolationException AccountNotFound(Guid accountId) =>
        new("Account.NotFound", $"No account in the ledger has id {accountId}.");

    public static DomainRuleViolationException CategoryNotFound(Guid categoryId) =>
        new("Category.NotFound", $"No category in the ledger has id {categoryId}.");
}
