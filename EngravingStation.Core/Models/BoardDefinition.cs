namespace EngravingStation.Core.Models;

public sealed record BoardDefinition(decimal WidthMm, decimal HeightMm, decimal SlotWidthMm, decimal SlotHeightMm, int Columns, int Rows, decimal MarginMm = 0m)
{
    public int Capacity => Columns * Rows;
    public decimal UsableWidthMm => WidthMm - (MarginMm * 2m);
    public decimal UsableHeightMm => HeightMm - (MarginMm * 2m);
    public decimal GridWidthMm => Columns * SlotWidthMm;
    public decimal GridHeightMm => Rows * SlotHeightMm;

    public static BoardDefinition Default { get; } = new(300m, 220m, 50m, 36m, 5, 5, 10m);
}
