namespace FinNavis.Application.Import.GetImportRun;

public sealed record SetAsidePageRequest(string? After, string? Before, int Limit)
{
    public const int MinLimit = 1;

    public const int MaxLimit = 200;

    public const int DefaultLimit = 50;

    public static SetAsidePageRequest FirstPage { get; } = new(null, null, DefaultLimit);
}
