using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.Errors;

namespace FinNavis.Application.Reporting;

// Public because the cursor is decoded by the adapter that minted it, so the adapter refuses a
// cursor it did not mint and has to reach the same refusal as the use case.
public static class HistoryErrors
{
    private const string Code = "History.InvalidFilter";

    public static DomainRuleViolationException DaysOutOfOrder() =>
        new(Code, "'from' must not be after 'to'.");

    public static DomainRuleViolationException LimitOutOfRange() =>
        new(
            Code,
            $"'limit' must be between {HistoryPageRequest.MinLimit} and {HistoryPageRequest.MaxLimit}.");

    public static DomainRuleViolationException BothCursorsGiven() =>
        new(Code, "A page is read in one direction: pass 'after' or 'before', not both.");

    public static DomainRuleViolationException CursorNotMinted() =>
        new(Code, "That is not a cursor this server handed out.");
}
