using EngravingStation.Core.Exceptions;
using EngravingStation.Core.Models;
using EngravingStation.Core.Repositories;
using Microsoft.Data.Sqlite;

namespace EngravingStation.Infrastructure.Repositories;

public sealed class SqliteOrderAssetRepository : IOrderAssetRepository
{
    private readonly string _connectionString;

    public SqliteOrderAssetRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<OrderAssetRecord>> FindByCodeAsync(string normalizedCode, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            var command = connection.CreateCommand();
            command.CommandText = """
                SELECT order_no, tracking_no, asset_path, width_mm, height_mm, version
                FROM asset_mappings
                WHERE upper(order_no) = $code OR upper(tracking_no) = $code
                """;
            command.Parameters.AddWithValue("$code", normalizedCode.ToUpperInvariant());

            var records = new List<OrderAssetRecord>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                records.Add(new OrderAssetRecord(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetDecimal(3),
                    reader.GetDecimal(4),
                    reader.GetString(5)));
            }

            return records;
        }
        catch (SqliteException exception)
        {
            throw new RepositoryException("SQLite lookup failed.", exception);
        }
    }
}
