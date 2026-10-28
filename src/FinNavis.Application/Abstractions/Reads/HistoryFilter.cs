namespace FinNavis.Application.Abstractions.Reads;

/// <summary>
/// What the owner asked history to narrow down to. Every filter is optional — a value of
/// <c>null</c> everywhere asks for the whole history (sad.md §6 flow 8).
/// </summary>
/// <param name="From">First calendar day of the period, inclusive, or <c>null</c> for no lower bound.</param>
/// <param name="To">Last calendar day of the period, inclusive, or <c>null</c> for no upper bound.</param>
/// <param name="AccountId">
/// Keep only movements touching this account. For a transfer that means either side.
/// </param>
/// <param name="CategoryId">
/// Keep only transactions filed under this category. <b>Setting it excludes every transfer</b>,
/// because a transfer carries no category, so returning one would mean the page did not all match
/// what was asked (settled at the api-forge gate).
/// </param>
/// <remarks>
/// <b>The two days are not a <c>DateRange</c>.</b> That type is a closed range and needs both ends;
/// here each end is optional on its own, which is what the contract offers. The use case builds a
/// <c>DateRange</c> only when both are given, and turns a bad pair into
/// <c>History.InvalidFilter</c>.
/// </remarks>
public sealed record HistoryFilter(
    DateOnly? From,
    DateOnly? To,
    Guid? AccountId,
    Guid? CategoryId);
