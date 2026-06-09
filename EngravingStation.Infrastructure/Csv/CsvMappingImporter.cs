using System.Globalization;
using EngravingStation.Core.Exceptions;
using EngravingStation.Core.Models;

namespace EngravingStation.Infrastructure.Csv;

public sealed class CsvMappingImporter
{
    private static readonly string[] RequiredColumns = ["order_no", "tracking_no", "asset_path", "width_mm", "height_mm", "version"];

    public async Task<IReadOnlyList<OrderAssetRecord>> ImportAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            using var reader = new StreamReader(stream);
            return await ImportAsync(reader, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException exception)
        {
            throw new CsvImportException($"CSV mapping import failed for '{path}'.", exception);
        }
    }

    public async Task<IReadOnlyList<OrderAssetRecord>> ImportAsync(TextReader reader, CancellationToken cancellationToken = default)
    {
        var records = await CsvRecordReader.ReadRecordsAsync(reader, cancellationToken).ConfigureAwait(false);
        if (records.Count == 0)
        {
            return [];
        }

        var header = CsvRecordReader.ReadHeader(records[0], RequiredColumns);
        return records.Skip(1).Select(record => CreateRecord(record, header)).ToArray();
    }

    private static OrderAssetRecord CreateRecord(CsvRecord record, IReadOnlyDictionary<string, int> header)
    {
        var orderNo = CsvRecordReader.GetRequiredColumn(record, header, "order_no");
        var trackingNo = CsvRecordReader.GetRequiredColumn(record, header, "tracking_no");
        var assetPath = CsvRecordReader.GetRequiredColumn(record, header, "asset_path");
        var widthMm = ParseDecimal(record, header, "width_mm");
        var heightMm = ParseDecimal(record, header, "height_mm");
        var version = CsvRecordReader.GetRequiredColumn(record, header, "version");

        return new OrderAssetRecord(orderNo, trackingNo, assetPath, widthMm, heightMm, version);
    }

    private static decimal ParseDecimal(CsvRecord record, IReadOnlyDictionary<string, int> header, string columnName)
    {
        var raw = CsvRecordReader.GetRequiredColumn(record, header, columnName);
        if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            throw new CsvImportException($"CSV line {record.LineNumber} has invalid decimal value '{raw}' for column '{columnName}'.");
        }

        return value;
    }
}
