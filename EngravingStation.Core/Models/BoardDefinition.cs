namespace EngravingStation.Core.Models;

public sealed record BoardDefinition(decimal WidthMm, decimal HeightMm, decimal SlotWidthMm, decimal SlotHeightMm, int Columns, int Rows)
{
    public int Capacity => Columns * Rows;

    public static BoardDefinition Default { get; } = new(300m, 200m, 60m, 40m, 5, 5);
}
