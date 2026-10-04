namespace FinNavis.Application.Common;

/// <summary>
/// A use-case failure. Carries only Application and primitive types, never a Domain type, so
/// Presentation never has to name one to translate an error into a response.
/// </summary>
public sealed record Error(ErrorType Type, string Title, string Detail, IReadOnlyDictionary<string, string[]>? FieldErrors = null)
{
    public static Error Validation(IReadOnlyDictionary<string, string[]> fieldErrors)
    {
        return new Error(ErrorType.Validation, "Validation failed.", "One or more fields are invalid.", fieldErrors);
    }

    public static Error Validation(string field, string message)
    {
        return Validation(new Dictionary<string, string[]> { [field] = [message] });
    }

    public static Error Conflict(string detail)
    {
        return new Error(ErrorType.Conflict, "Conflict.", detail);
    }
}
