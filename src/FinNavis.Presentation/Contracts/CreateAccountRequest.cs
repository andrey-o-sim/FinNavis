namespace FinNavis.Presentation.Contracts;

/// <summary>
/// Body of <c>POST /accounts</c>. <see cref="Type"/> is a string: a bad value reaches our own
/// validation instead of failing JSON binding with a non-ProblemDetails 400.
/// </summary>
public sealed record CreateAccountRequest(string? Name, string? Type, decimal OpeningBalance, string? Note);
