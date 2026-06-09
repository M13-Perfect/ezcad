using EngravingStation.Core.Models;

namespace EngravingStation.Core.Repositories;

public interface IOrderAssetRepository
{
    Task<IReadOnlyList<OrderAssetRecord>> FindByCodeAsync(string normalizedCode, CancellationToken cancellationToken = default);
}
