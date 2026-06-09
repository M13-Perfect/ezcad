using EngravingStation.Core.Models;
using EngravingStation.Core.Services;
using Xunit;

namespace EngravingStation.Core.Tests;

public sealed class LayoutTests
{
    [Fact]
    public void Generate_AssignsFixedSlotsInOrder()
    {
        var batch = CreateBatch(3, 10, 10);
        var result = new FixedSlotLayoutService().Generate(batch, new BoardDefinition(100, 100, 20, 20, 2, 2));
        Assert.True(result.Succeeded);
        Assert.Equal((0, 0), (result.Value!.Slots[0].Row, result.Value.Slots[0].Column));
        Assert.Equal((0, 1), (result.Value.Slots[1].Row, result.Value.Slots[1].Column));
        Assert.Equal((1, 0), (result.Value.Slots[2].Row, result.Value.Slots[2].Column));
    }

    [Fact]
    public void Generate_DetectsLayoutOverflowWhenCapacityExceeded()
    {
        var batch = CreateBatch(5, 10, 10);
        var result = new FixedSlotLayoutService().Generate(batch, new BoardDefinition(40, 40, 20, 20, 2, 2));
        Assert.False(result.Succeeded);
        Assert.Contains(result.Issues, issue => issue.Code == "LAYOUT_OVERFLOW");
    }

    [Fact]
    public void Generate_DetectsSlotSizeOverflow()
    {
        var batch = CreateBatch(1, 30, 10);
        var result = new FixedSlotLayoutService().Generate(batch, new BoardDefinition(40, 40, 20, 20, 2, 2));
        Assert.False(result.Succeeded);
        Assert.Contains(result.Issues, issue => issue.Code == "LAYOUT_OVERFLOW");
    }

    private static Batch CreateBatch(int count, decimal width, decimal height)
    {
        var batch = new Batch();
        for (var index = 0; index < count; index++)
        {
            var item = new BatchItem($"ORD-{1000 + index}", $"ORD-{1000 + index}")
            {
                Status = QueueItemStatus.Valid,
                Asset = new OrderAssetRecord($"ORD-{1000 + index}", $"TRK{index:000000}", $"asset-{index}.svg", width, height, "v1")
            };
            batch.AddItem(item);
        }

        return batch;
    }
}
