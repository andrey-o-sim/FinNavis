namespace FinNavis.Application.Common;

/// <summary>
/// The kind of failure a use case can report. Two members, because those are the two failure
/// modes a use case in this slice produces; a missing-by-id result is expressed as <c>null</c>
/// instead, see <c>GetAccountByIdUseCase</c>.
/// </summary>
public enum ErrorType
{
    Validation,
    Conflict,
}
