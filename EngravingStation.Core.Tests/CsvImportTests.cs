using EngravingStation.Infrastructure.Csv;
using Xunit;

namespace EngravingStation.Core.Tests;

public sealed class CsvImportTests
{
    [Fact]
    public async Task ImportAsync_ReadsMappingRows()
    {
        using var reader = new StringReader("order_no,tracking_no,asset_path,width_mm,height_mm,version\nORD-1,TRK1,asset.svg,10.5,7,v1\n");
        var records = await new CsvMappingImporter().ImportAsync(reader);
        Assert.Single(records);
        Assert.Equal("ORD-1", records[0].OrderNo);
        Assert.Equal(10.5m, records[0].WidthMm);
    }
}
