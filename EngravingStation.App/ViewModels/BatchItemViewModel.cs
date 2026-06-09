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
    public string Status => Item.Status.ToString();
    public string Detail => Item.ErrorMessage ?? Item.Asset?.AssetPath ?? string.Empty;

    public void Refresh()
    {
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(Detail));
    }
}
