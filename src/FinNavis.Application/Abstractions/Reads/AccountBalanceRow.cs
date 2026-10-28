using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Abstractions.Reads;

/// <summary>
/// One account and what it holds right now. A read row, not an <c>Account</c> entity.
/// </summary>
/// <param name="AccountId">The account the figure belongs to.</param>
/// <param name="Name">The account's name, joined in so the caller needs no second read.</param>
/// <param name="Balance">
/// The sum of every movement recorded against the account. May be negative, so it is a signed
/// figure (AC-14).
/// </param>
/// <remarks>
/// Nothing here is stored. The figure is computed from the movements at read time (ADR-0002), which
/// is what makes an edit or a removal free: there is no stored total to correct (AC-21, AC-22).
/// <para>
/// <b>The total across all accounts is not here.</b> The use case adds the per-account figures
/// together (sad.md §6 flow 7), so a transfer between the owner's own accounts leaves it unchanged
/// by construction (AC-15).
/// </para>
/// </remarks>
public sealed record AccountBalanceRow(Guid AccountId, string Name, Money Balance);
