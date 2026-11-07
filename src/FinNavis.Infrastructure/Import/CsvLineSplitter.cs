using System.Text;

namespace FinNavis.Infrastructure.Import;

internal static class CsvLineSplitter
{
    private const char Separator = ',';
    private const char Quote = '"';

    public static List<string> Split(string line)
    {
        List<string> values = [];
        StringBuilder value = new();
        bool insideQuotes = false;

        for (int index = 0; index < line.Length; index++)
        {
            char current = line[index];

            if (insideQuotes)
            {
                bool isEscapedQuote = current == Quote && index + 1 < line.Length && line[index + 1] == Quote;
                if (isEscapedQuote)
                {
                    value.Append(Quote);
                    index++;
                }
                else if (current == Quote)
                {
                    insideQuotes = false;
                }
                else
                {
                    value.Append(current);
                }
            }
            else if (current == Quote)
            {
                insideQuotes = true;
            }
            else if (current == Separator)
            {
                values.Add(value.ToString());
                value.Clear();
            }
            else
            {
                value.Append(current);
            }
        }

        values.Add(value.ToString());
        return values;
    }
}
