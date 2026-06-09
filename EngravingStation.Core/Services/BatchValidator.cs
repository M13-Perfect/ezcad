using EngravingStation.Core.Models;
using EngravingStation.Core.Results;

namespace EngravingStation.Core.Services;

public sealed class BatchValidator
{
    private readonly OrderCodeValidator _codeValidator;
    private readonly AssetResolver _assetResolver;

    public BatchValidator(OrderCodeValidator codeValidator, AssetResolver assetResolver)
    {
        _codeValidator = codeValidator;
        _assetResolver = assetResolver;
    }

    public async Task<OperationResult> ValidateAsync(Batch batch, CancellationToken cancellationToken = default)
    {
        var result = new OperationResult();
        if (batch.Items.Count == 0)
        {
            result.AddError("EMPTY_BATCH", "Add at least one scan or CSV row before validation.");
            return result;
        }

        var duplicates = batch.Items.GroupBy(item => item.NormalizedCode).Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
        foreach (var item in batch.Items)
        {
            item.Status = QueueItemStatus.Pending;
            item.ErrorMessage = null;
            item.Asset = null;
            item.Slot = null;

            if (string.IsNullOrWhiteSpace(item.NormalizedCode))
            {
                item.Status = QueueItemStatus.Invalid;
                item.ErrorMessage = "Scan input is empty after normalization.";
                result.AddError("EMPTY_SCAN_INPUT", item.ErrorMessage);
                continue;
            }

            if (!_codeValidator.IsValid(item.NormalizedCode))
            {
                item.Status = QueueItemStatus.Invalid;
                item.ErrorMessage = $"{item.NormalizedCode} does not match any configured order-code rule.";
                result.AddError("INVALID_ORDER_CODE", item.ErrorMessage);
                continue;
            }

            if (duplicates.Contains(item.NormalizedCode))
            {
                item.Status = QueueItemStatus.Duplicate;
                item.ErrorMessage = $"{item.NormalizedCode} appears more than once in this batch.";
                result.AddError("DUPLICATE_ORDER_CODE", item.ErrorMessage);
                continue;
            }

            var assetResult = await _assetResolver.ResolveAsync(item.NormalizedCode, cancellationToken).ConfigureAwait(false);
            if (!assetResult.Succeeded || assetResult.Value is null)
            {
                var issue = assetResult.Issues.First(issue => issue.Severity == OperationIssueSeverity.Error);
                item.Status = issue.Code switch
                {
                    "MULTIPLE_ASSETS" => QueueItemStatus.MultipleAssets,
                    "MISSING_FILE" or "ORDER_NOT_FOUND" => QueueItemStatus.MissingAsset,
                    "UNSUPPORTED_FILE_TYPE" => QueueItemStatus.UnsupportedAsset,
                    _ => QueueItemStatus.Error
                };
                item.ErrorMessage = issue.Message;
                result.AddError(issue.Code, issue.Message);
                continue;
            }

            item.Asset = assetResult.Value;
            item.Status = QueueItemStatus.Valid;
        }

        return result;
    }
}
