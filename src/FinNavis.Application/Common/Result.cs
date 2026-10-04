namespace FinNavis.Application.Common;

/// <summary>
/// The outcome of a use case that can fail: either a value, or an <see cref="Common.Error"/>
/// describing why not.
/// </summary>
public sealed class Result<TValue>
{
    private readonly TValue? _value;
    private readonly Error? _error;

    private Result(bool isSuccess, TValue? value, Error? error)
    {
        IsSuccess = isSuccess;
        _value = value;
        _error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    /// <summary>The value on success. Throws when the result is a failure.</summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Result has no value because it is a failure.");

    /// <summary>The error on failure. Throws when the result is a success.</summary>
    public Error Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Result has no error because it is a success.");

    public static Result<TValue> Success(TValue value)
    {
        return new Result<TValue>(true, value, null);
    }

    public static Result<TValue> Failure(Error error)
    {
        return new Result<TValue>(false, default, error);
    }
}
