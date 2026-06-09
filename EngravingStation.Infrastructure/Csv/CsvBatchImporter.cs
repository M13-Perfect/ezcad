using EngravingStation.Core.Exceptions;
using EngravingStation.Core.Models;
using EngravingStation.Core.Services;

namespace EngravingStation.Infrastructure.Csv;

public sealed class CsvBatchImporter
{
    private static readonly string[] RequiredColumns = ["order_code"];
    private readonly OrderCodeNormalizer _normalizer;

    public CsvBatchImporter()
        : this(new OrderCodeNormalizer())
    {
    }

    public CsvBatchImporter(OrderCodeNormalizer normalizer)
    {
        _normalizer = normalizer;
    }

    public async Task<IReadOnlyList<BatchItem>> ImportAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            using var reader = new StreamReader(stream);
            return await ImportAsync(reader, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException exception)
        {
            throw new CsvImportException($"CSV batch import failed for '{path}'.", exception);
        }
    }

    public async Task<IReadOnlyList<BatchItem>> ImportAsync(TextReader reader, CancellationToken cancellationToken = default)
    {
        var records = await CsvRecordReader.ReadRecordsAsync(reader, cancellationToken).ConfigureAwait(false);
        if (records.Count == 0)
        {
            return [];
        }

        var header = CsvRecordReader.ReadHeader(records[0], RequiredColumns);
        return records
            .Skip(1)
            .Select(record => CsvRecordReader.GetRequiredColumn(record, header, "order_code"))
            .Select(orderCode => new BatchItem(orderCode, _normalizer.Normalize(orderCode)))
            .ToArray();
    }
}
