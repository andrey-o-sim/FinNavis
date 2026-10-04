namespace FinNavis.Presentation.Contracts;

/// <summary>
/// Body returned for an account. Exactly these five members: no creation timestamp is ever
/// returned.
/// </summary>
public sealed record AccountResponse(Guid Id, string Name, string Type, decimal OpeningBalance, string? Note);
