using EngravingStation.Core.Models;
using EngravingStation.Core.Repositories;
using EngravingStation.Core.Results;

namespace EngravingStation.Core.Services;

public sealed class AssetResolver
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".svg", ".dxf", ".ai", ".pdf" };
    private readonly IOrderAssetRepository _repository;
    private readonly IFileSystem _fileSystem;

    public AssetResolver(IOrderAssetRepository repository, IFileSystem fileSystem)
    {
        _repository = repository;
        _fileSystem = fileSystem;
    }

    public async Task<OperationResult<OrderAssetRecord>> ResolveAsync(string normalizedCode, CancellationToken cancellationToken = default)
    {
        var matches = await _repository.FindByCodeAsync(normalizedCode, cancellationToken).ConfigureAwait(false);
        if (matches.Count == 0)
        {
            return OperationResult<OrderAssetRecord>.Failure(new OperationIssue("ORDER_NOT_FOUND", $"No asset mapping was found for {normalizedCode}.", OperationIssueSeverity.Error));
        }

        var distinctMatches = matches.DistinctBy(match => Path.GetFullPath(match.AssetPath)).ToArray();
        if (distinctMatches.Length > 1)
        {
            return OperationResult<OrderAssetRecord>.Failure(new OperationIssue("MULTIPLE_ASSETS", $"Multiple assets match {normalizedCode}; operator must choose a single mapping.", OperationIssueSeverity.Error));
        }

        var asset = distinctMatches[0];
        if (asset.WidthMm <= 0 || asset.HeightMm <= 0)
        {
            return OperationResult<OrderAssetRecord>.Failure(new OperationIssue("INVALID_DIMENSIONS", $"Asset dimensions for {normalizedCode} must be greater than zero.", OperationIssueSeverity.Error));
        }

        if (!SupportedExtensions.Contains(Path.GetExtension(asset.AssetPath)))
        {
            return OperationResult<OrderAssetRecord>.Failure(new OperationIssue("UNSUPPORTED_FILE_TYPE", $"{asset.AssetPath} uses an unsupported file extension.", OperationIssueSeverity.Error));
        }

        if (!_fileSystem.FileExists(asset.AssetPath))
        {
            return OperationResult<OrderAssetRecord>.Failure(new OperationIssue("MISSING_FILE", $"{asset.AssetPath} does not exist.", OperationIssueSeverity.Error));
        }

        return OperationResult<OrderAssetRecord>.Success(asset);
    }
}

public interface IFileSystem
{
    bool FileExists(string path);
}
