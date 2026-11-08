namespace FinNavis.Presentation.Errors;

public static class ErrorStatusCodes
{
    // Keyed by the code string because each code is raised where its rule lives — in an entity or
    // a use case — and is never defined a second time here. A test compares these keys and statuses
    // with the codes src raises and with the responses the contract lists them under.
    private static readonly Dictionary<string, int> StatusByCode = new()
    {
        ["Account.NameInvalid"] = StatusCodes.Status400BadRequest,
        ["Account.NotFound"] = StatusCodes.Status404NotFound,
        ["Account.SourceNotFound"] = StatusCodes.Status404NotFound,
        ["Account.DestinationNotFound"] = StatusCodes.Status404NotFound,
        ["Account.DuplicateName"] = StatusCodes.Status409Conflict,
        ["Account.HasRecordedHistory"] = StatusCodes.Status409Conflict,

        ["Category.NameInvalid"] = StatusCodes.Status400BadRequest,
        ["Category.NotFound"] = StatusCodes.Status404NotFound,
        ["Category.DuplicateName"] = StatusCodes.Status409Conflict,
        ["Category.GroupKindMismatch"] = StatusCodes.Status409Conflict,
        ["Category.HasRecordedHistory"] = StatusCodes.Status409Conflict,

        ["CategoryGroup.NameInvalid"] = StatusCodes.Status400BadRequest,
        ["CategoryGroup.NameReserved"] = StatusCodes.Status400BadRequest,
        ["CategoryGroup.NotFound"] = StatusCodes.Status404NotFound,
        ["CategoryGroup.DuplicateName"] = StatusCodes.Status409Conflict,

        ["Transaction.AmountNotPositive"] = StatusCodes.Status400BadRequest,
        ["Transaction.AmountOutOfRange"] = StatusCodes.Status400BadRequest,
        ["Transaction.NoteTooLong"] = StatusCodes.Status400BadRequest,
        ["Transaction.KindChangeNotAllowed"] = StatusCodes.Status400BadRequest,
        ["Transaction.NotFound"] = StatusCodes.Status404NotFound,

        ["Transfer.SameAccount"] = StatusCodes.Status400BadRequest,
        ["Transfer.AmountNotPositive"] = StatusCodes.Status400BadRequest,
        ["Transfer.AmountOutOfRange"] = StatusCodes.Status400BadRequest,
        ["Transfer.CategoryNotAllowed"] = StatusCodes.Status400BadRequest,
        ["Transfer.NotFound"] = StatusCodes.Status404NotFound,
        ["Transfer.NotRecorded"] = StatusCodes.Status500InternalServerError,

        ["History.InvalidFilter"] = StatusCodes.Status400BadRequest,

        ["Breakdown.InvalidPeriod"] = StatusCodes.Status400BadRequest,
    };

    public static IReadOnlyDictionary<string, int> All => StatusByCode;

    public static bool TryGetStatus(string code, out int status) => StatusByCode.TryGetValue(code, out status);
}
