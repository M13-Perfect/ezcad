namespace EngravingStation.Core.Models;

public sealed record LayoutSlot(int Index, int Row, int Column, decimal Xmm, decimal Ymm, decimal WidthMm, decimal HeightMm, string AssetPath, string Code);
