using FinNavis.Domain.Errors;

namespace FinNavis.Application.Transfers;

/// <summary>
/// The refusals the transfer use cases raise around the entity.
/// </summary>
/// <remarks>
/// The wording is the PRD acceptance criterion, not invented (sad.md §8).
/// <para>
/// <b>The two account refusals carry the <c>Account</c> module</b>, not this one. The module of a
/// <c>*.NotFound</c> code names the aggregate that was not found (<c>docs/adr/0004</c>). The role
/// stays in the name because one request names the same aggregate twice, and the owner has to
/// know which of the two pickers to correct.
/// </para>
/// </remarks>
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
