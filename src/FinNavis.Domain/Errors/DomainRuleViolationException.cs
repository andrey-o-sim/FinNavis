namespace FinNavis.Domain.Errors;

/// <summary>
/// The one error a Domain entity raises when it refuses. There is no exception type per rule:
/// the rule is told apart by <see cref="Code"/>, not by the type (sad.md §8).
/// </summary>
/// <remarks>
/// Presentation maps this to the flat envelope <c>{code, message, details?}</c>. The code strings
/// themselves live in Presentation's registry, so Domain carries the code it was given and never
/// the HTTP spelling of it.
/// <para>
/// A use case raises it too, for a refusal an entity cannot see on its own — an id the ledger does
/// not hold, or a commit that did not go through (AC-06). The rule is still told apart by
/// <see cref="Code"/>, so Presentation has one thing to catch whoever refused.
/// </para>
/// </remarks>
public sealed class DomainRuleViolationException : Exception
{
    public DomainRuleViolationException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>
    /// The same refusal, keeping what actually failed underneath it. Used where the owner reads a
    /// sentence the acceptance criteria fix while the cause is a technical one worth logging.
    /// </summary>
    public DomainRuleViolationException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    /// <summary>
    /// The stable code of the broken rule, as <c>Module.ErrorName</c> — for example
    /// <c>Account.NameInvalid</c>. A client switches on this value.
    /// </summary>
    public string Code { get; }
}
