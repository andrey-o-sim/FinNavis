namespace FinNavis.Application.Abstractions.Reads;

public sealed record HistoryPageRequest(string? After, string? Before, int Limit)
{
    public const int MinLimit = 1;

    public const int MaxLimit = 200;

    public const int DefaultLimit = 50;
}
