namespace FinNavis.Application.Transactions;

/// <summary>
/// The fields an edit changes. Anything left unset keeps the value the entry already carries
/// (AC-21).
/// </summary>
/// <param name="AccountId">The account to move the entry to, or unset to leave it where it is.</param>
/// <param name="SecondAccountId">
/// A further account the owner named. The HTTP contract has no property for it, so this is
/// <c>null</c> on every normal request — it exists so AC-24 is refused by the entity as well as by
/// the request shape, and no surface can turn a transaction into a transfer by going around that
/// shape.
/// </param>
/// <param name="CategoryId">The category to file the entry under, or unset to keep the current one.</param>
/// <param name="Amount">The new amount, or unset to keep the current one.</param>
/// <param name="OccurredOn">The new calendar day, or unset to keep the current one.</param>
/// <param name="Note">
/// What to do with the note. Unset keeps it; <see cref="NoteChange.To"/> with <c>null</c> clears
/// it.
/// </param>
public sealed record EditTransactionChanges(
    Guid? AccountId = null,
    Guid? SecondAccountId = null,
    Guid? CategoryId = null,
    decimal? Amount = null,
    DateOnly? OccurredOn = null,
    NoteChange Note = default);
