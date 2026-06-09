using EngravingStation.Core.Models;
using EngravingStation.Core.Results;

namespace EngravingStation.Cad;

public interface ICadAdapter
{
    Task<OperationResult> ImportLayoutAsync(BoardLayout layout, CancellationToken cancellationToken = default);
}
