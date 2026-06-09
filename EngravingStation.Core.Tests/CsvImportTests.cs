using EngravingStation.Core.Exceptions;
using EngravingStation.Core.Models;
using EngravingStation.Core.Results;
using EngravingStation.Core.Services;
using EngravingStation.Infrastructure.Csv;
using EngravingStation.Infrastructure.Repositories;
using Xunit;

namespace EngravingStation.Core.Tests;

public sealed class CsvImportTests
{
    [Fact]
    public async Task MappingImportAsync_ReadsAssetMappingRows()
    {
        using var reader = new StringReader("order_no,tracking_no,asset_path,width_mm,height_mm,version\nORD-1,TRK1,asset.svg,10.5,7,v1\n");
        var records = await new CsvMappingImporter().ImportAsync(reader);
        Assert.Single(records);
        Assert.Equal("ORD-1", records[0].OrderNo);
        Assert.Equal(10.5m, records[0].WidthMm);
    }

    [Fact]
    public async Task BatchImportAsync_ReadsAndNormalizesBatchRows()
    {
        using var reader = new StringReader("order_code\n ord-1001 \ntrK000002\n");
        var rows = await new CsvBatchImporter().ImportAsync(reader);
        Assert.Equal(["ORD-1001", "TRK000002"], rows.Select(row => row.NormalizedCode));
        Assert.Equal(" ord-1001 ", rows[0].RawInput);
    }

    [Fact]
    public async Task BatchImportAsync_RejectsMissingOrderCodeColumn()
    {
        using var reader = new StringReader("order_no\nORD-1001\n");
        var exception = await Assert.ThrowsAsync<CsvImportException>(() => new CsvBatchImporter().ImportAsync(reader));
        Assert.Contains("order_code", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MappingImportAsync_RejectsInvalidDimensions()
    {
        using var reader = new StringReader("order_no,tracking_no,asset_path,width_mm,height_mm,version\nORD-1001,TRK000001,asset.svg,nope,7,v1\n");
        var exception = await Assert.ThrowsAsync<CsvImportException>(() => new CsvMappingImporter().ImportAsync(reader));
        Assert.Contains("width_mm", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CsvBatchValidation_DetectsDuplicateOrderCodes()
    {
        var result = await ValidateCsvBatchAsync(
            "order_code\nORD-1001\n ord-1001 \n",
            "order_no,tracking_no,asset_path,width_mm,height_mm,version\nORD-1001,TRK000001,asset.svg,10,10,v1\n",
            ["asset.svg"]);

        Assert.False(result.Validation.Succeeded);
        Assert.Contains(result.Validation.Issues, issue => issue.Code == "DUPLICATE_ORDER_CODE");
        Assert.All(result.Batch.Items, item => Assert.Equal(QueueItemStatus.Duplicate, item.Status));
    }

    [Fact]
    public async Task CsvBatchValidation_DetectsMissingMapping()
    {
        var result = await ValidateCsvBatchAsync(
            "order_code\nORD-9999\n",
            "order_no,tracking_no,asset_path,width_mm,height_mm,version\nORD-1001,TRK000001,asset.svg,10,10,v1\n",
            ["asset.svg"]);

        Assert.False(result.Validation.Succeeded);
        Assert.Contains(result.Validation.Issues, issue => issue.Code == "ORDER_NOT_FOUND");
        Assert.Equal(QueueItemStatus.MissingAsset, result.Batch.Items[0].Status);
    }

    [Fact]
    public async Task CsvBatchValidation_DetectsMissingFile()
    {
        var result = await ValidateCsvBatchAsync(
            "order_code\nORD-1001\n",
            "order_no,tracking_no,asset_path,width_mm,height_mm,version\nORD-1001,TRK000001,missing.svg,10,10,v1\n",
            []);

        Assert.False(result.Validation.Succeeded);
        Assert.Contains(result.Validation.Issues, issue => issue.Code == "MISSING_FILE");
        Assert.Equal(QueueItemStatus.MissingAsset, result.Batch.Items[0].Status);
    }

    [Fact]
    public async Task CsvBatchValidation_DetectsUnsupportedExtension()
    {
        var result = await ValidateCsvBatchAsync(
            "order_code\nORD-1001\n",
            "order_no,tracking_no,asset_path,width_mm,height_mm,version\nORD-1001,TRK000001,asset.bmp,10,10,v1\n",
            ["asset.bmp"]);

        Assert.False(result.Validation.Succeeded);
        Assert.Contains(result.Validation.Issues, issue => issue.Code == "UNSUPPORTED_FILE_TYPE");
        Assert.Equal(QueueItemStatus.UnsupportedAsset, result.Batch.Items[0].Status);
    }

    [Fact]
    public async Task CsvBatchValidation_DetectsMultipleAssetMatches()
    {
        var result = await ValidateCsvBatchAsync(
            "order_code\nORD-1001\n",
            "order_no,tracking_no,asset_path,width_mm,height_mm,version\nORD-1001,TRK000001,a.svg,10,10,v1\nORD-1001,TRK000001,b.svg,10,10,v2\n",
            ["a.svg", "b.svg"]);

        Assert.False(result.Validation.Succeeded);
        Assert.Contains(result.Validation.Issues, issue => issue.Code == "MULTIPLE_ASSETS");
        Assert.Equal(QueueItemStatus.MultipleAssets, result.Batch.Items[0].Status);
    }

    private static async Task<(Batch Batch, OperationResult Validation)> ValidateCsvBatchAsync(string batchCsv, string mappingCsv, IEnumerable<string> existingFiles)
    {
        using var batchReader = new StringReader(batchCsv);
        using var mappingReader = new StringReader(mappingCsv);
        var batchRows = await new CsvBatchImporter().ImportAsync(batchReader);
        var mappingRows = await new CsvMappingImporter().ImportAsync(mappingReader);
        var batch = new Batch();
        foreach (var row in batchRows)
        {
            batch.AddItem(row);
        }

        var resolver = new AssetResolver(new InMemoryOrderAssetRepository(mappingRows), new FakeFileSystem(existingFiles));
        var validator = new BatchValidator(OrderCodeValidator.Default, resolver);
        var validation = await validator.ValidateAsync(batch);
        return (batch, validation);
    }
}
