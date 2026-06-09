using System.Text;
using EngravingStation.Core.Exceptions;

namespace EngravingStation.Infrastructure.Csv;

internal sealed record CsvRecord(int LineNumber, IReadOnlyList<string> Columns);

internal static class CsvRecordReader
{
    public static async Task<IReadOnlyList<CsvRecord>> ReadRecordsAsync(TextReader reader, CancellationToken cancellationToken)
    {
        var records = new List<CsvRecord>();
        var lineNumber = 0;
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            records.Add(new CsvRecord(lineNumber, SplitCsv(line, lineNumber)));
        }

        return records;
    }

    public static Dictionary<string, int> ReadHeader(CsvRecord headerRecord, IReadOnlyCollection<string> requiredColumns)
    {
        var header = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < headerRecord.Columns.Count; index++)
        {
            var name = headerRecord.Columns[index].Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            header.TryAdd(name, index);
        }

        var missingColumns = requiredColumns.Where(column => !header.ContainsKey(column)).ToArray();
        if (missingColumns.Length > 0)
        {
            throw new CsvImportException($"CSV header is missing required column(s): {string.Join(", ", missingColumns)}.");
        }

        return header;
    }

    public static string GetRequiredColumn(CsvRecord record, IReadOnlyDictionary<string, int> header, string columnName)
    {
        if (!header.TryGetValue(columnName, out var index) || index >= record.Columns.Count)
        {
            throw new CsvImportException($"CSV line {record.LineNumber} is missing value for required column '{columnName}'.");
        }

        var value = record.Columns[index].Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new CsvImportException($"CSV line {record.LineNumber} has an empty value for required column '{columnName}'.");
        }

        return value;
    }

    private static IReadOnlyList<string> SplitCsv(string line, int lineNumber)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (character == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }

        if (inQuotes)
        {
            throw new CsvImportException($"CSV line {lineNumber} has an unterminated quoted field.");
        }

        values.Add(current.ToString());
        return values;
    }
}
