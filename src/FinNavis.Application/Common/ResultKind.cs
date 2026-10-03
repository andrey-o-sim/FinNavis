namespace FinNavis.Application.Common;

/// <summary>
/// How a use case ended. Presentation maps each value to an HTTP status.
/// </summary>
public enum ResultKind
{
    Success,
    Invalid,
    NotFound,
}
