namespace FinNavis.Application.Transfers;

/// <summary>
/// The fields an edit changes. Anything left unset keeps the value the transfer already carries
/// (AC-21).
/// </summary>
/// <param name="SourceAccountId">The account the money leaves, or unset to keep the current one.</param>
/// <param name="DestinationAccountId">The account the money arrives in, or unset to keep the current one.</param>
/// <param name="Amount">The new amount, or unset to keep the current one.</param>
/// <param name="OccurredOn">The new calendar day, or unset to keep the current one.</param>
/// <param name="CategoryId">
/// A category the owner tried to give the transfer. The HTTP contract has no property for it and
/// the entity has no field for it, so this is <c>null</c> on every normal request — it exists so
/// the attempt is refused with the AC-23 sentence instead of being ignored.
/// </param>
public sealed record EditTransferChanges(
    Guid? SourceAccountId = null,
    Guid? DestinationAccountId = null,
    decimal? Amount = null,
    DateOnly? OccurredOn = null,
    Guid? CategoryId = null);
