using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace FinNavis.Infrastructure.Queries;

public readonly record struct HistoryCursor(DateOnly OccurredOn, Guid Id)
{
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
