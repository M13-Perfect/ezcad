using EngravingStation.Core.Models;
using EngravingStation.Core.Services;
using EngravingStation.Infrastructure.Repositories;
using Xunit;

namespace EngravingStation.Core.Tests;

public sealed class BatchValidatorTests
{
    [Fact]
    public async Task ValidateAsync_DetectsDuplicateCodes()
    {
        var batch = new Batch();
        batch.AddItem(new BatchItem("ORD-1001", "ORD-1001"));
        batch.AddItem(new BatchItem("ord-1001", "ORD-1001"));
        var validator = CreateValidator();
        var result = await validator.ValidateAsync(batch);
        Assert.False(result.Succeeded);
        Assert.All(batch.Items, item => Assert.Equal(QueueItemStatus.Duplicate, item.Status));
    }

    [Fact]
    public async Task ValidateAsync_ValidatesAndResolvesAssets()
    {
        var batch = new Batch();
        batch.AddItem(new BatchItem("ORD-1001", "ORD-1001"));
        var validator = CreateValidator();
        var result = await validator.ValidateAsync(batch);
        Assert.True(result.Succeeded);
        Assert.Equal(QueueItemStatus.Valid, batch.Items[0].Status);
        Assert.NotNull(batch.Items[0].Asset);
    }

    private static BatchValidator CreateValidator()
    {
        var records = new[] { new OrderAssetRecord("ORD-1001", "TRK000001", "asset.svg", 10, 10, "v1") };
        var resolver = new AssetResolver(new InMemoryOrderAssetRepository(records), new FakeFileSystem(["asset.svg"]));
        return new BatchValidator(new OrderCodeValidator(["^ORD-[0-9]{4}$"]), resolver);
    }
}
