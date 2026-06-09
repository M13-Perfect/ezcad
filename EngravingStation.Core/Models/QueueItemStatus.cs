namespace EngravingStation.Core.Models;

public enum QueueItemStatus
{
    Pending,
    Valid,
    Duplicate,
    Invalid,
    MissingAsset,
    MultipleAssets,
    UnsupportedAsset,
    LayoutAssigned,
    Error
}
