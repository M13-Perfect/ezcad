using System.Globalization;
using System.Text;
using System.Text.Json;
using EngravingStation.Core.Exceptions;
using EngravingStation.Core.Models;
using EngravingStation.Core.Results;

namespace EngravingStation.Cad;

public sealed class LayoutFileCadAdapter : ICadAdapter
{
    private readonly string _outputDirectory;

    public LayoutFileCadAdapter(string outputDirectory)
    {
        _outputDirectory = outputDirectory;
    }

    public async Task<OperationResult> ImportLayoutAsync(BoardLayout layout, CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(_outputDirectory);
            var jsonPath = Path.Combine(_outputDirectory, "job.json");
            await using (var stream = File.Create(jsonPath))
            {
                await JsonSerializer.SerializeAsync(stream, layout, new JsonSerializerOptions { WriteIndented = true }, cancellationToken).ConfigureAwait(false);
            }

            await File.WriteAllTextAsync(Path.Combine(_outputDirectory, "layout-preview.svg"), CreateSvg(layout), cancellationToken).ConfigureAwait(false);
            return new OperationResult();
        }
        catch (IOException exception)
        {
            throw new CadAdapterException("Failed to write CAD layout files.", exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new CadAdapterException("CAD layout output directory is not writable.", exception);
        }
    }

    private static string CreateSvg(BoardLayout layout)
    {
        var culture = CultureInfo.InvariantCulture;
        var builder = new StringBuilder();
        builder.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{layout.Board.WidthMm.ToString(culture)}mm\" height=\"{layout.Board.HeightMm.ToString(culture)}mm\" viewBox=\"0 0 {layout.Board.WidthMm.ToString(culture)} {layout.Board.HeightMm.ToString(culture)}\">");
        builder.AppendLine("  <rect x=\"0\" y=\"0\" width=\"100%\" height=\"100%\" fill=\"white\" stroke=\"black\" />");
        foreach (var slot in layout.Slots)
        {
            builder.AppendLine($"  <rect x=\"{slot.Xmm.ToString(culture)}\" y=\"{slot.Ymm.ToString(culture)}\" width=\"{slot.WidthMm.ToString(culture)}\" height=\"{slot.HeightMm.ToString(culture)}\" fill=\"#dbeafe\" stroke=\"#2563eb\" />");
            builder.AppendLine($"  <text x=\"{(slot.Xmm + 2).ToString(culture)}\" y=\"{(slot.Ymm + 8).ToString(culture)}\" font-size=\"4\">{System.Security.SecurityElement.Escape(slot.Code)}</text>");
        }

        builder.AppendLine("</svg>");
        return builder.ToString();
    }
}
