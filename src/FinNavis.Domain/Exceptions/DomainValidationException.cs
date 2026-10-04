namespace FinNavis.Domain.Exceptions;

/// <summary>
/// Thrown when a Domain entity cannot be constructed because one or more of its rules failed.
/// Carries every broken rule at once, keyed by the field name, so a caller does not have to
/// retry one error at a time.
/// </summary>
public sealed class DomainValidationException : Exception
{
    public DomainValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more domain validation rules failed.")
    {
        Errors = errors;
    }

    /// <summary>Broken rules, keyed by field name (for example "name", "note").</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
