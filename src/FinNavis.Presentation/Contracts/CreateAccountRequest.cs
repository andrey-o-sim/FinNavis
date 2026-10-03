namespace FinNavis.Presentation.Contracts;

/// <summary>
/// Body of <c>POST /accounts</c>.
/// </summary>
/// <remarks>
/// <c>Type</c> is a plain string, not the <c>AccountType</c> enum. With the enum here, an
/// unknown value would fail in the JSON binder and return a 400 whose message names a Domain
/// type. A string lets the use case answer with a field error on "type" instead.
/// </remarks>
public sealed record CreateAccountRequest(string Name, string Type, decimal InitialBalance);
