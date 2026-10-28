namespace FinNavis.Domain.Enums;

/// <summary>
/// Which side of the ledger a category sits on. A group carries the same value (AC-26, AC-30).
/// </summary>
/// <remarks>
/// Stored as a string, never as an int (CLAUDE.md), so the numbers below never reach the database.
/// They start at 1 so a default-initialised value is not a valid side by accident.
/// </remarks>
public enum CategoryKind
{
    /// <summary>Money leaving the ledger.</summary>
    Expense = 1,

    /// <summary>Money arriving in the ledger.</summary>
    Income = 2,
}
