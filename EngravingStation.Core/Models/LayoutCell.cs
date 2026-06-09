namespace EngravingStation.Core.Models;

public sealed record LayoutCell(int Index, int Row, int Column, decimal Xmm, decimal Ymm, decimal WidthMm, decimal HeightMm);
