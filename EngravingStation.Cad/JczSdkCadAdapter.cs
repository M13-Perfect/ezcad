using EngravingStation.Core.Exceptions;
using EngravingStation.Core.Models;
using EngravingStation.Core.Results;

namespace EngravingStation.Cad;

public sealed class JczSdkCadAdapter : ICadAdapter
{
    public Task<OperationResult> ImportLayoutAsync(BoardLayout layout, CancellationToken cancellationToken = default)
    {
        throw new CadAdapterException("JCZ SDK integration is intentionally not implemented until official SDK files, headers, or examples are added to the repository.");
    }
}
