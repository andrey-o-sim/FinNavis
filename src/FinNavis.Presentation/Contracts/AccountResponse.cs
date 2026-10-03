namespace FinNavis.Presentation.Contracts;

/// <summary>
/// An account as the API returns it.
/// </summary>
/// <remarks>
/// Field for field the same as <c>AccountDto</c> today. The duplication is on purpose: the
/// wire contract is allowed to change without touching a use case, and a Domain or Application
/// type never leaks out of an endpoint. See docs/adr/0001-initial-setup.md.
/// </remarks>
public sealed record AccountResponse(
    Guid Id,
    string Name,
    string Type,
    decimal InitialBalance,
    decimal Balance,
    DateTimeOffset CreatedAt);
