using EngravingStation.Core.Models;

namespace EngravingStation.App.ViewModels;

public sealed class PreviewSlotViewModel
{
    private const decimal Scale = 2m;

    public PreviewSlotViewModel(LayoutSlot slot)
    {
        Code = slot.Code;
        X = (double)(slot.Xmm * Scale);
        Y = (double)(slot.Ymm * Scale);
        Width = (double)(slot.WidthMm * Scale);
        Height = (double)(slot.HeightMm * Scale);
    }

    public string Code { get; }
    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }
}
