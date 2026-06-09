namespace EngravingStation.Core.Models;

public sealed record LayoutSlot(
    int Index,
    int Row,
    int Column,
    decimal Xmm,
    decimal Ymm,
    decimal WidthMm,
    decimal HeightMm,
    string AssetPath,
    string Code,
    decimal SlotXmm,
    decimal SlotYmm,
    decimal SlotWidthMm,
    decimal SlotHeightMm);
