namespace EngravingStation.Core.Models;

public sealed class BatchItem
{
    public BatchItem(string rawInput, string normalizedCode)
    {
        RawInput = rawInput;
        NormalizedCode = normalizedCode;
    }

    public string RawInput { get; }
    public string NormalizedCode { get; }
    public QueueItemStatus Status { get; set; } = QueueItemStatus.Pending;
    public string? ErrorMessage { get; set; }
    public OrderAssetRecord? Asset { get; set; }
    public LayoutSlot? Slot { get; set; }
}
