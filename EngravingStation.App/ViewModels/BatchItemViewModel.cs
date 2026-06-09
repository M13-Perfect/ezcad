using EngravingStation.Core.Models;

namespace EngravingStation.App.ViewModels;

public sealed class BatchItemViewModel : ViewModelBase
{
    public BatchItemViewModel(BatchItem item)
    {
        Item = item;
    }

    public BatchItem Item { get; }
    public string Code => Item.NormalizedCode;
    public string RawInput => Item.RawInput;
    public string Status => Item.Status.ToString();
    public string Detail => Item.ErrorMessage ?? Item.Asset?.AssetPath ?? SlotSummary;
    public string ErrorMessage => Item.ErrorMessage ?? string.Empty;
    public string AssetPath => Item.Asset?.AssetPath ?? string.Empty;
    public string SlotSummary => Item.Slot is null
        ? string.Empty
        : $"Slot {Item.Slot.Index + 1} (row {Item.Slot.Row + 1}, column {Item.Slot.Column + 1}) at {Item.Slot.Xmm:0.##} mm, {Item.Slot.Ymm:0.##} mm";

    public void Refresh()
    {
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(Detail));
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(AssetPath));
        OnPropertyChanged(nameof(SlotSummary));
    }
}
