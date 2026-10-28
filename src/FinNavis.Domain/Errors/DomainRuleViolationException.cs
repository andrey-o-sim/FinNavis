namespace FinNavis.Domain.Errors;

/// <summary>
/// The one error a Domain entity raises when it refuses. There is no exception type per rule:
/// the rule is told apart by <see cref="Code"/>, not by the type (sad.md §8).
/// </summary>
/// <remarks>
/// Presentation maps this to the flat envelope <c>{code, message, details?}</c>. The code strings
/// themselves live in Presentation's registry, so Domain carries the code it was given and never
/// the HTTP spelling of it.
/// </remarks>
public sealed class DomainRuleViolationException : Exception
{
    public DomainRuleViolationException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>
    /// The stable code of the broken rule, as <c>Module.ErrorName</c> — for example
    /// <c>Account.NameInvalid</c>. A client switches on this value.
    /// </summary>
    public string Code { get; }
}
