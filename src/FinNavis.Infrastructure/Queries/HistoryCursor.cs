using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace FinNavis.Infrastructure.Queries;

/// <summary>
/// Where one page of history ends: the movement the next page starts after, named by the pair the
/// page is ordered on.
/// </summary>
/// <param name="OccurredOn">The calendar day of the boundary movement.</param>
/// <param name="Id">That movement's id — a time-ordered UUID v7, the tiebreaker inside one day.</param>
/// <remarks>
/// <b>The token is opaque and the server is the only side that reads it</b>
/// (<c>.claude/rules/openapi.md</c> deviation 3). History is a <c>UNION ALL</c> over two tables
/// (ADR-0001), so one bare id cannot locate a boundary: the day has to travel with it. A client
/// that tried to build a token out of a row id gets it refused by <see cref="TryDecode"/>.
/// </remarks>
public readonly record struct HistoryCursor(DateOnly OccurredOn, Guid Id)
{
    /// <summary>
    /// The longest token the contract advertises, <c>string(200)</c>. A longer one is refused
    /// before it is decoded, so a large value cannot be used to make the server work.
    /// </summary>
    public const int MaxTokenLength = 200;

    // Carried inside the token so a later change to the page order can refuse the old shape
    // instead of silently reading it as the new one.
    private const string Version = "v1";

    private const char Separator = ':';

    private const string DayFormat = "yyyy-MM-dd";

    public string Encode()
    {
        string plain = string.Join(
            Separator,
            Version,
            OccurredOn.ToString(DayFormat, CultureInfo.InvariantCulture),
            Id.ToString("D", CultureInfo.InvariantCulture));

        return Base64Url.EncodeToString(Encoding.UTF8.GetBytes(plain));
    }

    public static bool TryDecode(string? token, out HistoryCursor cursor)
    {
        cursor = default;

        if (string.IsNullOrWhiteSpace(token) || token.Length > MaxTokenLength)
        {
            return false;
        }

        if (!Base64Url.IsValid(token))
        {
            return false;
        }

        string plain;
        try
        {
            plain = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(token));
        }
        catch (FormatException)
        {
            return false;
        }

        string[] parts = plain.Split(Separator);

        if (parts.Length != 3 || parts[0] != Version)
        {
            return false;
        }

        if (!DateOnly.TryParseExact(parts[1], DayFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly day))
        {
            return false;
        }

        if (!Guid.TryParseExact(parts[2], "D", out Guid id))
        {
            return false;
        }

        cursor = new HistoryCursor(day, id);
        return true;
    }
}
