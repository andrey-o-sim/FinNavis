namespace FinNavis.Application.Features.Accounts;

/// <summary>
/// Input for <see cref="CreateAccount"/>.
/// </summary>
/// <remarks>
/// <c>Type</c> is a string, not <c>AccountType</c>. An unknown type has to come back as a
/// field error on "type", and an enum parameter would move that failure into the JSON binder
/// before the use case ever runs.
/// </remarks>
public sealed record CreateAccountCommand(string Name, string Type, decimal InitialBalance);
