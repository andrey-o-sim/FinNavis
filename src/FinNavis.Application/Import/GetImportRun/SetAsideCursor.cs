using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace FinNavis.Application.Import.GetImportRun;

// The run id travels inside the token so that a cursor minted for one run is refused on another,
// instead of silently paging the wrong run from the same row number.
internal readonly record struct SetAsideCursor(Guid ImportRunId, int RowNumber)
{
    public const int MaxTokenLength = 200;

    // Carried inside the token so a later change to the page order can refuse the old shape
    // instead of silently reading it as the new one.
    private const string Version = "v1";

    private const char Separator = ':';

    public string Encode()
    {
        string plain = string.Join(
            Separator,
            Version,
            ImportRunId.ToString("D", CultureInfo.InvariantCulture),
            RowNumber.ToString(CultureInfo.InvariantCulture));

        return Base64Url.EncodeToString(Encoding.UTF8.GetBytes(plain));
    }

    public static bool TryDecode(string token, out SetAsideCursor cursor)
    {
        cursor = default;

        if (string.IsNullOrWhiteSpace(token) || token.Length > MaxTokenLength || !Base64Url.IsValid(token))
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

        if (!Guid.TryParseExact(parts[1], "D", out Guid importRunId))
        {
            return false;
        }

        if (!int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out int rowNumber))
        {
            return false;
        }

        cursor = new SetAsideCursor(importRunId, rowNumber);
        return true;
    }
}
