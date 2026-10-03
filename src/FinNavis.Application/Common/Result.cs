namespace FinNavis.Application.Common;

/// <summary>
/// What a use case returns: a value, a set of field errors, or "not found".
/// </summary>
/// <remarks>
/// This is how a broken rule reaches HTTP without an exception. Domain owns the rule and its
/// wording; the use case attaches a field name and returns <see cref="Invalid(string, string)"/>;
/// Presentation turns that into a validation problem response.
/// Deliberately small — no Map, Bind or Match. Three endpoints do not need a mini-monad.
/// </remarks>
/// <typeparam name="TValue">The type returned on success.</typeparam>
public sealed class Result<TValue>
{
    private static readonly IReadOnlyDictionary<string, string[]> NoErrors =
        new Dictionary<string, string[]>();

    private readonly TValue? _value;

    private Result(ResultKind kind, TValue? value, IReadOnlyDictionary<string, string[]> errors)
    {
        Kind = kind;
        _value = value;
        Errors = errors;
    }

    public ResultKind Kind { get; }

    public bool IsSuccess => Kind == ResultKind.Success;

    /// <summary>Field name to messages. Empty unless <see cref="Kind"/> is Invalid.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>The value. Reading it on a failed result is a programmer error.</summary>
    /// <exception cref="InvalidOperationException">The result is not a success.</exception>
    public TValue Value
    {
        get
        {
            if (!IsSuccess)
            {
                throw new InvalidOperationException($"A {Kind} result has no value.");
            }

            return _value!;
        }
    }

    public static Result<TValue> Success(TValue value)
    {
        return new Result<TValue>(ResultKind.Success, value, NoErrors);
    }

    /// <summary>One broken rule on one field.</summary>
    public static Result<TValue> Invalid(string field, string message)
    {
        return Invalid(new Dictionary<string, string[]> { [field] = [message] });
    }

    /// <summary>Several broken rules at once, one entry per field.</summary>
    public static Result<TValue> Invalid(IReadOnlyDictionary<string, string[]> errors)
    {
        if (errors.Count == 0)
        {
            throw new ArgumentException("An invalid result needs at least one error.", nameof(errors));
        }

        return new Result<TValue>(ResultKind.Invalid, default, errors);
    }

    public static Result<TValue> NotFound()
    {
        return new Result<TValue>(ResultKind.NotFound, default, NoErrors);
    }
}
