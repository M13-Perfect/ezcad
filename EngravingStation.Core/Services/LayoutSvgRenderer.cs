using System.Globalization;
using System.Security;
using System.Text;
using EngravingStation.Core.Models;

namespace EngravingStation.Core.Services;

public static class LayoutSvgRenderer
{
    public static string Render(BoardLayout layout)
    {
        var culture = CultureInfo.InvariantCulture;
        var builder = new StringBuilder();
        builder.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{Format(layout.Board.WidthMm, culture)}mm\" height=\"{Format(layout.Board.HeightMm, culture)}mm\" viewBox=\"0 0 {Format(layout.Board.WidthMm, culture)} {Format(layout.Board.HeightMm, culture)}\">");
        builder.AppendLine("  <rect x=\"0\" y=\"0\" width=\"100%\" height=\"100%\" fill=\"white\" stroke=\"black\" />");
        builder.AppendLine($"  <rect x=\"{Format(layout.Board.MarginMm, culture)}\" y=\"{Format(layout.Board.MarginMm, culture)}\" width=\"{Format(layout.Board.UsableWidthMm, culture)}\" height=\"{Format(layout.Board.UsableHeightMm, culture)}\" fill=\"none\" stroke=\"#94a3b8\" stroke-dasharray=\"2 2\" />");

        foreach (var cell in layout.Cells)
        {
            builder.AppendLine($"  <rect x=\"{Format(cell.Xmm, culture)}\" y=\"{Format(cell.Ymm, culture)}\" width=\"{Format(cell.WidthMm, culture)}\" height=\"{Format(cell.HeightMm, culture)}\" fill=\"#f8fafc\" stroke=\"#cbd5e1\" />");
        }

        foreach (var slot in layout.Slots)
        {
            builder.AppendLine($"  <rect x=\"{Format(slot.Xmm, culture)}\" y=\"{Format(slot.Ymm, culture)}\" width=\"{Format(slot.WidthMm, culture)}\" height=\"{Format(slot.HeightMm, culture)}\" fill=\"#dbeafe\" stroke=\"#2563eb\" />");
            builder.AppendLine($"  <text x=\"{Format(slot.Xmm + 2m, culture)}\" y=\"{Format(slot.Ymm + 8m, culture)}\" font-size=\"4\">{SecurityElement.Escape(slot.Code) ?? string.Empty}</text>");
        }

        builder.AppendLine("</svg>");
        return builder.ToString();
    }

    private static string Format(decimal value, CultureInfo culture) => value.ToString("0.###", culture);
}
