using EngravingStation.Core.Models;

namespace EngravingStation.App.ViewModels;

public sealed class PreviewCellViewModel
{
    public PreviewCellViewModel(LayoutCell cell)
    {
        X = (double)(cell.Xmm * PreviewSlotViewModel.Scale);
        Y = (double)(cell.Ymm * PreviewSlotViewModel.Scale);
        Width = (double)(cell.WidthMm * PreviewSlotViewModel.Scale);
        Height = (double)(cell.HeightMm * PreviewSlotViewModel.Scale);
    }

    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }
}
