using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Reporting;

/// <summary>
/// The period's totals: group figures and category figures, the two sides of the ledger kept
/// apart (AC-18, AC-19, AC-20).
/// </summary>
/// <param name="Period">The days totalled. Both ends count as inside.</param>
/// <param name="AccountId">The account filter that produced this answer, or <c>null</c> for every account.</param>
/// <param name="Expense">Spending.</param>
/// <param name="Income">Earning.</param>
/// <remarks>
/// <b>No transfer is counted on either side</b> (AC-20). The query behind it reads
/// <c>transactions</c> and never touches <c>transfers</c>, so this holds structurally rather than
/// by a filter someone could forget (ADR-0001).
/// </remarks>
public sealed record Breakdown(
    DateRange Period,
    Guid? AccountId,
    BreakdownSide Expense,
    BreakdownSide Income);
