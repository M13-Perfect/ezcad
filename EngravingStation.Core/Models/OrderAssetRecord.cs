namespace EngravingStation.Core.Models;

public sealed record OrderAssetRecord(
    string OrderNo,
    string TrackingNo,
    string AssetPath,
    decimal WidthMm,
    decimal HeightMm,
    string Version);
