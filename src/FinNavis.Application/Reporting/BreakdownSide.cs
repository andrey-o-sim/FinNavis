using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Reporting;

/// <summary>
/// One side of the ledger. Spending and earning are never added together (AC-18).
/// </summary>
/// <param name="Total">
/// The side's figure. Every group's total, the Default group included, adds up to it (AC-19).
/// </param>
/// <param name="Groups">The groups that recorded something in the period.</param>
/// <remarks>
/// A group that recorded nothing in the period has no entry here. It is left out rather than
/// reported as zero — PRD §8's own stated default, settled at the api-forge gate.
/// </remarks>
public sealed record BreakdownSide(Money Total, IReadOnlyList<BreakdownGroupTotal> Groups);
