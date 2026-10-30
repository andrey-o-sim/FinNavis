using FinNavis.Domain.Errors;

namespace FinNavis.Application.Transactions;

internal static class TransactionErrors
{
    public static DomainRuleViolationException NotFound(Guid transactionId) =>
        new("Transaction.NotFound", $"No transaction in the ledger has id {transactionId}.");

    public static DomainRuleViolationException AccountNotFound(Guid accountId) =>
        new("Account.NotFound", $"No account in the ledger has id {accountId}.");

    public static DomainRuleViolationException CategoryNotFound(Guid categoryId) =>
        new("Category.NotFound", $"No category in the ledger has id {categoryId}.");
}
