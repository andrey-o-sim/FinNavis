using FinNavis.Domain.Enums;

namespace FinNavis.Application.Common;

/// <summary>
/// Spells a side of the ledger the way a refusal message names it.
/// </summary>
/// <remarks>
/// The wording comes from the acceptance criteria, so it has to read the same in every refusal
/// that names a side. "An expense" and "An income" both read correctly, so the article never
/// varies and the caller can write it in front.
/// </remarks>
internal static class CategoryKindText
{
    public static string AsLedgerSide(this CategoryKind kind) => kind.ToString().ToLowerInvariant();
}
