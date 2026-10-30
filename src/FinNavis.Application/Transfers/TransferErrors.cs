using FinNavis.Domain.Errors;

namespace FinNavis.Application.Transfers;

internal static class TransferErrors
{
    public static DomainRuleViolationException NotFound(Guid transferId) =>
        new("Transfer.NotFound", $"No transfer in the ledger has id {transferId}.");

    public static DomainRuleViolationException SourceAccountNotFound(Guid accountId) =>
        new("Account.SourceNotFound", $"No account in the ledger has id {accountId}.");

    public static DomainRuleViolationException DestinationAccountNotFound(Guid accountId) =>
        new("Account.DestinationNotFound", $"No account in the ledger has id {accountId}.");

    public static DomainRuleViolationException CategoryNotAllowed() =>
        new("Transfer.CategoryNotAllowed", "Transfers carry no category.");

    public static DomainRuleViolationException NotRecorded(Exception failure) =>
        new(
            "Transfer.NotRecorded",
            "The transfer could not be recorded. Neither account changed.",
            failure);
}
