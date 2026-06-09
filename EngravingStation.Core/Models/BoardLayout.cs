namespace EngravingStation.Core.Models;

public sealed record BoardLayout(BoardDefinition Board, IReadOnlyList<LayoutSlot> Slots, IReadOnlyList<LayoutCell> Cells);
