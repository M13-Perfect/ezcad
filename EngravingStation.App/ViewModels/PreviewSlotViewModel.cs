using EngravingStation.Core.Models;

namespace EngravingStation.App.ViewModels;

public sealed class PreviewSlotViewModel
{
    public const decimal Scale = 2m;

    public PreviewSlotViewModel(LayoutSlot slot)
    {
        Code = slot.Code;
        X = (double)(slot.Xmm * Scale);
        Y = (double)(slot.Ymm * Scale);
        Width = (double)(slot.WidthMm * Scale);
        Height = (double)(slot.HeightMm * Scale);
        SlotX = (double)(slot.SlotXmm * Scale);
        SlotY = (double)(slot.SlotYmm * Scale);
        SlotWidth = (double)(slot.SlotWidthMm * Scale);
        SlotHeight = (double)(slot.SlotHeightMm * Scale);
    }

    public string Code { get; }
    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }
    public double SlotX { get; }
    public double SlotY { get; }
    public double SlotWidth { get; }
    public double SlotHeight { get; }
}
