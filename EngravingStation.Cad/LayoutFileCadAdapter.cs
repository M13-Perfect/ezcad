using System.Text.Json;
using EngravingStation.Core.Exceptions;
using EngravingStation.Core.Models;
using EngravingStation.Core.Results;
using EngravingStation.Core.Services;

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

    private static string CreateSvg(BoardLayout layout) => LayoutSvgRenderer.Render(layout);
}
