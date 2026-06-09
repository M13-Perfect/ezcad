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
        Assert.Equal(4, result.Value.Cells.Count);
    }

    [Fact]
    public void Generate_AppliesConfiguredMarginAndCentersAssetsInSlots()
    {
        var batch = CreateBatch(1, 10, 8);
        var result = new FixedSlotLayoutService().Generate(batch, new BoardDefinition(100, 80, 20, 16, 3, 2, 5));
        Assert.True(result.Succeeded);
        var slot = result.Value!.Slots[0];
        Assert.Equal(5, slot.SlotXmm);
        Assert.Equal(5, slot.SlotYmm);
        Assert.Equal(10, slot.Xmm);
        Assert.Equal(9, slot.Ymm);
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

    [Fact]
    public void Generate_DetectsGridOverflowOutsideBoardMargin()
    {
        var batch = CreateBatch(1, 10, 10);
        var result = new FixedSlotLayoutService().Generate(batch, new BoardDefinition(50, 50, 20, 20, 2, 2, 8));
        Assert.False(result.Succeeded);
        Assert.Contains(result.Issues, issue => issue.Code == "LAYOUT_OVERFLOW");
    }

    [Fact]
    public void Generate_DetectsInvalidBoardDimensions()
    {
        var batch = CreateBatch(1, 10, 10);
        var result = new FixedSlotLayoutService().Generate(batch, new BoardDefinition(50, 50, 0, 20, 2, 2));
        Assert.False(result.Succeeded);
        Assert.Contains(result.Issues, issue => issue.Code == "INVALID_DIMENSIONS");
    }

    [Fact]
    public void Render_IncludesSlotCellsAndAssetsInDebugSvg()
    {
        var batch = CreateBatch(1, 10, 10);
        var layout = new FixedSlotLayoutService().Generate(batch, new BoardDefinition(100, 100, 20, 20, 2, 2, 5)).Value!;
        var svg = LayoutSvgRenderer.Render(layout);
        Assert.Contains("<svg", svg);
        Assert.Contains("stroke=\"#cbd5e1\"", svg);
        Assert.Contains("ORD-1000", svg);
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
