using EngravingStation.Core.Models;
using EngravingStation.Core.Results;

namespace EngravingStation.Core.Services;

public sealed class FixedSlotLayoutService
{
    public OperationResult<BoardLayout> Generate(Batch batch, BoardDefinition board)
    {
        var boardValidationIssue = ValidateBoard(board);
        if (boardValidationIssue is not null)
        {
            return OperationResult<BoardLayout>.Failure(boardValidationIssue);
        }

        var validItems = batch.Items.Where(item => item.Status == QueueItemStatus.Valid && item.Asset is not null).ToArray();
        if (validItems.Length > board.Capacity)
        {
            return OperationResult<BoardLayout>.Failure(new OperationIssue("LAYOUT_OVERFLOW", $"Batch has {validItems.Length} valid items but the board only has {board.Capacity} slots.", OperationIssueSeverity.Error));
        }

        var cells = CreateCells(board);
        var slots = new List<LayoutSlot>(validItems.Length);
        var occupiedCells = new HashSet<(int Row, int Column)>();
        var occupiedAssetBounds = new List<LayoutRectangle>(validItems.Length);
        for (var index = 0; index < validItems.Length; index++)
        {
            var item = validItems[index];
            var asset = item.Asset!;
            if (asset.WidthMm <= 0m || asset.HeightMm <= 0m)
            {
                return OperationResult<BoardLayout>.Failure(new OperationIssue("INVALID_DIMENSIONS", $"{item.NormalizedCode} has invalid asset dimensions.", OperationIssueSeverity.Error));
            }

            if (asset.WidthMm > board.SlotWidthMm || asset.HeightMm > board.SlotHeightMm)
            {
                return OperationResult<BoardLayout>.Failure(new OperationIssue("LAYOUT_OVERFLOW", $"{item.NormalizedCode} is larger than the fixed slot size.", OperationIssueSeverity.Error));
            }

            var row = index / board.Columns;
            var column = index % board.Columns;
            if (!occupiedCells.Add((row, column)))
            {
                return OperationResult<BoardLayout>.Failure(new OperationIssue("LAYOUT_COLLISION", $"Slot {row},{column} is already occupied.", OperationIssueSeverity.Error));
            }

            var cell = cells[index];
            var slotX = cell.Xmm;
            var slotY = cell.Ymm;
            var x = slotX + ((board.SlotWidthMm - asset.WidthMm) / 2m);
            var y = slotY + ((board.SlotHeightMm - asset.HeightMm) / 2m);
            var assetBounds = new LayoutRectangle(x, y, asset.WidthMm, asset.HeightMm);

            if (!IsInsideBoardUsableArea(assetBounds, board))
            {
                return OperationResult<BoardLayout>.Failure(new OperationIssue("LAYOUT_OVERFLOW", $"{item.NormalizedCode} would extend outside the board usable area.", OperationIssueSeverity.Error));
            }

            if (occupiedAssetBounds.Any(existing => existing.Intersects(assetBounds)))
            {
                return OperationResult<BoardLayout>.Failure(new OperationIssue("LAYOUT_COLLISION", $"{item.NormalizedCode} overlaps another layout item.", OperationIssueSeverity.Error));
            }

            occupiedAssetBounds.Add(assetBounds);
            var slot = new LayoutSlot(index, row, column, x, y, asset.WidthMm, asset.HeightMm, asset.AssetPath, item.NormalizedCode, slotX, slotY, board.SlotWidthMm, board.SlotHeightMm);
            item.Slot = slot;
            item.Status = QueueItemStatus.LayoutAssigned;
            slots.Add(slot);
        }

        return OperationResult<BoardLayout>.Success(new BoardLayout(board, slots, cells));
    }

    private static IReadOnlyList<LayoutCell> CreateCells(BoardDefinition board)
    {
        var cells = new List<LayoutCell>(board.Capacity);
        for (var row = 0; row < board.Rows; row++)
        {
            for (var column = 0; column < board.Columns; column++)
            {
                var index = cells.Count;
                cells.Add(new LayoutCell(index, row, column, board.MarginMm + (column * board.SlotWidthMm), board.MarginMm + (row * board.SlotHeightMm), board.SlotWidthMm, board.SlotHeightMm));
            }
        }

        return cells;
    }

    private static OperationIssue? ValidateBoard(BoardDefinition board)
    {
        if (board.WidthMm <= 0m || board.HeightMm <= 0m || board.SlotWidthMm <= 0m || board.SlotHeightMm <= 0m || board.MarginMm < 0m)
        {
            return new OperationIssue("INVALID_DIMENSIONS", "Board size, slot width, and slot height must be positive values; margin cannot be negative.", OperationIssueSeverity.Error);
        }

        if (board.Columns <= 0 || board.Rows <= 0)
        {
            return new OperationIssue("INVALID_DIMENSIONS", "Board row and column counts must be positive values.", OperationIssueSeverity.Error);
        }

        if (board.UsableWidthMm <= 0m || board.UsableHeightMm <= 0m)
        {
            return new OperationIssue("LAYOUT_OVERFLOW", "Board margin leaves no usable layout area.", OperationIssueSeverity.Error);
        }

        if (board.GridWidthMm > board.UsableWidthMm || board.GridHeightMm > board.UsableHeightMm)
        {
            return new OperationIssue("LAYOUT_OVERFLOW", "Fixed-slot grid does not fit inside the configured board and margin.", OperationIssueSeverity.Error);
        }

        return null;
    }

    private static bool IsInsideBoardUsableArea(LayoutRectangle bounds, BoardDefinition board)
    {
        return bounds.X >= board.MarginMm
            && bounds.Y >= board.MarginMm
            && bounds.Right <= board.WidthMm - board.MarginMm
            && bounds.Bottom <= board.HeightMm - board.MarginMm;
    }

    private readonly record struct LayoutRectangle(decimal X, decimal Y, decimal Width, decimal Height)
    {
        public decimal Right => X + Width;
        public decimal Bottom => Y + Height;

        public bool Intersects(LayoutRectangle other)
        {
            return X < other.Right && Right > other.X && Y < other.Bottom && Bottom > other.Y;
        }
    }
}
