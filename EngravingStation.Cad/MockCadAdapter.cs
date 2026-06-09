using EngravingStation.Core.Models;
using EngravingStation.Core.Results;

namespace EngravingStation.Cad;

public sealed class MockCadAdapter : ICadAdapter
{
    public BoardLayout? LastImportedLayout { get; private set; }

    public Task<OperationResult> ImportLayoutAsync(BoardLayout layout, CancellationToken cancellationToken = default)
    {
        LastImportedLayout = layout;
        return Task.FromResult(new OperationResult());
    }
}
