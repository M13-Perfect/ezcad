using System.Globalization;
using System.Text;
using EngravingStation.Core.Models;

namespace EngravingStation.Infrastructure.Csv;

public sealed class CsvMappingImporter
{
    public async Task<IReadOnlyList<OrderAssetRecord>> ImportAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream);
        return await ImportAsync(reader, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OrderAssetRecord>> ImportAsync(TextReader reader, CancellationToken cancellationToken = default)
    {
        var records = new List<OrderAssetRecord>();
        var header = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        if (header is null)
        {
            return records;
        }

        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = SplitCsv(line);
            if (columns.Count < 6)
            {
                continue;
            }

            records.Add(new OrderAssetRecord(
                columns[0].Trim(),
                columns[1].Trim(),
                columns[2].Trim(),
                decimal.Parse(columns[3], CultureInfo.InvariantCulture),
                decimal.Parse(columns[4], CultureInfo.InvariantCulture),
                columns[5].Trim()));
        }

        return records;
    }

    private static IReadOnlyList<string> SplitCsv(string line)
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

        values.Add(current.ToString());
        return values;
    }
}
