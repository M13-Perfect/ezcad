using EngravingStation.Core.Models;
using EngravingStation.Core.Services;
using EngravingStation.Infrastructure.Repositories;
using Xunit;

namespace EngravingStation.Core.Tests;

public sealed class AssetResolverTests
{
    [Fact]
    public async Task ResolveAsync_ReturnsSingleSupportedExistingAsset()
    {
        var record = new OrderAssetRecord("ORD-1001", "TRK000001", "asset.svg", 10, 10, "v1");
        var resolver = CreateResolver([record], ["asset.svg"]);
        var result = await resolver.ResolveAsync("ORD-1001");
        Assert.True(result.Succeeded);
        Assert.Equal(record, result.Value);
    }

    [Fact]
    public async Task ResolveAsync_DoesNotGuessWhenMultipleAssetsMatch()
    {
        var resolver = CreateResolver([
            new OrderAssetRecord("ORD-1001", "TRK000001", "a.svg", 10, 10, "v1"),
            new OrderAssetRecord("ORD-1001", "TRK000001", "b.svg", 10, 10, "v2")
        ], ["a.svg", "b.svg"]);
        var result = await resolver.ResolveAsync("ORD-1001");
        Assert.False(result.Succeeded);
        Assert.Contains(result.Issues, issue => issue.Code == "MULTIPLE_ASSETS");
    }

    [Fact]
    public async Task ResolveAsync_RejectsMissingAndUnsupportedAssets()
    {
        var missing = CreateResolver([new OrderAssetRecord("ORD-1001", "TRK000001", "asset.svg", 10, 10, "v1")], []);
        Assert.Contains((await missing.ResolveAsync("ORD-1001")).Issues, issue => issue.Code == "MISSING_FILE");

        var unsupported = CreateResolver([new OrderAssetRecord("ORD-1002", "TRK000002", "asset.bmp", 10, 10, "v1")], ["asset.bmp"]);
        Assert.Contains((await unsupported.ResolveAsync("ORD-1002")).Issues, issue => issue.Code == "UNSUPPORTED_FILE_TYPE");
    }

    private static AssetResolver CreateResolver(IEnumerable<OrderAssetRecord> records, IEnumerable<string> files)
        => new(new InMemoryOrderAssetRepository(records), new FakeFileSystem(files));
}
