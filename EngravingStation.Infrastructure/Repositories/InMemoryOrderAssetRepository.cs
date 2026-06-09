using EngravingStation.Core.Models;
using EngravingStation.Core.Repositories;

namespace EngravingStation.Infrastructure.Repositories;

public sealed class InMemoryOrderAssetRepository : IOrderAssetRepository
{
    private readonly List<OrderAssetRecord> _records;

    public InMemoryOrderAssetRepository(IEnumerable<OrderAssetRecord> records)
    {
        _records = records.ToList();
    }

    public Task<IReadOnlyList<OrderAssetRecord>> FindByCodeAsync(string normalizedCode, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OrderAssetRecord> matches = _records
            .Where(record => string.Equals(record.OrderNo, normalizedCode, StringComparison.OrdinalIgnoreCase) || string.Equals(record.TrackingNo, normalizedCode, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        return Task.FromResult(matches);
    }
}
