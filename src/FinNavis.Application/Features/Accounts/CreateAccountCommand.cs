namespace FinNavis.Application.Features.Accounts;

/// <summary>
/// What it takes to create an account. <see cref="Type"/> is a string: parsing it against the
/// Domain enum is the use case's job, not the caller's.
/// </summary>
public sealed record CreateAccountCommand(string? Name, string? Type, decimal OpeningBalance, string? Note);
