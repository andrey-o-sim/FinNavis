namespace FinNavis.Domain.Errors;

public sealed class DomainRuleViolationException : Exception
{
    public DomainRuleViolationException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public DomainRuleViolationException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    public DomainRuleViolationException(
        string code,
        string message,
        IReadOnlyDictionary<string, object> details)
        : base(message)
    {
        throw new NotImplementedException();
    }

    public string Code { get; }

    public IReadOnlyDictionary<string, object>? Details { get; }
}
