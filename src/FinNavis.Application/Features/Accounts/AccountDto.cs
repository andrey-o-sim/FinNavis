namespace FinNavis.Application.Features.Accounts;

/// <summary>
/// An account, shaped for a caller outside Application. <see cref="Type"/> is a string so
/// Presentation never has to name the Domain <c>AccountType</c> enum. No timestamp: the creation
/// date is stored but never returned by any endpoint.
/// </summary>
public sealed record AccountDto(Guid Id, string Name, string Type, decimal OpeningBalance, string? Note);
