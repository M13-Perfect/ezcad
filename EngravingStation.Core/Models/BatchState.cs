namespace EngravingStation.Core.Models;

public enum BatchState
{
    Draft,
    Validating,
    ReadyToLayout,
    LayoutGenerated,
    LayoutConfirmed,
    ImportingToCad,
    ImportedToCad,
    OperatorApproved,
    Completed
}
