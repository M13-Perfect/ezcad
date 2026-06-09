using EngravingStation.Core.Models;
using EngravingStation.Core.Results;

namespace EngravingStation.Core.Services;

public sealed class FixedSlotLayoutService
{
    public OperationResult<BoardLayout> Generate(Batch batch, BoardDefinition board)
    {
        var validItems = batch.Items.Where(item => item.Status == QueueItemStatus.Valid && item.Asset is not null).ToArray();
        if (validItems.Length > board.Capacity)
        {
            return OperationResult<BoardLayout>.Failure(new OperationIssue("LAYOUT_OVERFLOW", $"Batch has {validItems.Length} valid items but the board only has {board.Capacity} slots.", OperationIssueSeverity.Error));
        }

        var slots = new List<LayoutSlot>(validItems.Length);
        var occupied = new HashSet<(int Row, int Column)>();
        for (var index = 0; index < validItems.Length; index++)
        {
            var item = validItems[index];
            var asset = item.Asset!;
            if (asset.WidthMm > board.SlotWidthMm || asset.HeightMm > board.SlotHeightMm)
            {
                return OperationResult<BoardLayout>.Failure(new OperationIssue("LAYOUT_OVERFLOW", $"{item.NormalizedCode} is larger than the fixed slot size.", OperationIssueSeverity.Error));
            }

            var row = index / board.Columns;
            var column = index % board.Columns;
            if (!occupied.Add((row, column)))
            {
                return OperationResult<BoardLayout>.Failure(new OperationIssue("LAYOUT_COLLISION", $"Slot {row},{column} is already occupied.", OperationIssueSeverity.Error));
            }

            var slot = new LayoutSlot(index, row, column, column * board.SlotWidthMm, row * board.SlotHeightMm, asset.WidthMm, asset.HeightMm, asset.AssetPath, item.NormalizedCode);
            item.Slot = slot;
            item.Status = QueueItemStatus.LayoutAssigned;
            slots.Add(slot);
        }

        return OperationResult<BoardLayout>.Success(new BoardLayout(board, slots));
    }
}
