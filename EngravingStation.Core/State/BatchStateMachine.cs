using EngravingStation.Core.Exceptions;
using EngravingStation.Core.Models;
using EngravingStation.Core.Results;

namespace EngravingStation.Core.State;

public sealed class BatchStateMachine
{
    private static readonly IReadOnlyDictionary<BatchState, BatchState> NextStates = new Dictionary<BatchState, BatchState>
    {
        [BatchState.Draft] = BatchState.Validating,
        [BatchState.Validating] = BatchState.ReadyToLayout,
        [BatchState.ReadyToLayout] = BatchState.LayoutGenerated,
        [BatchState.LayoutGenerated] = BatchState.LayoutConfirmed,
        [BatchState.LayoutConfirmed] = BatchState.ImportingToCad,
        [BatchState.ImportingToCad] = BatchState.ImportedToCad,
        [BatchState.ImportedToCad] = BatchState.OperatorApproved,
        [BatchState.OperatorApproved] = BatchState.Completed
    };

    public void MoveNext(Batch batch, OperationResult? guardResult = null)
    {
        if (guardResult is { Succeeded: false })
        {
            throw new BatchStateException($"Cannot leave {batch.State} while validation contains errors.");
        }

        if (!NextStates.TryGetValue(batch.State, out var next))
        {
            throw new BatchStateException($"Batch state {batch.State} has no next state.");
        }

        batch.State = next;
    }

    public void ConfirmLayout(Batch batch)
    {
        EnsureState(batch, BatchState.LayoutGenerated);
        batch.LayoutManuallyConfirmed = true;
        batch.State = BatchState.LayoutConfirmed;
    }

    public void EnsureCanImportToCad(Batch batch, OperationResult latestValidation)
    {
        EnsureState(batch, BatchState.LayoutConfirmed);
        if (!batch.LayoutManuallyConfirmed)
        {
            throw new BatchStateException("Manual layout confirmation is required before CAD import.");
        }

        if (!latestValidation.Succeeded || latestValidation.HasWarnings)
        {
            throw new BatchStateException("CAD import is blocked until validation has no warnings or errors.");
        }
    }

    public void FinalOperatorApproval(Batch batch)
    {
        EnsureState(batch, BatchState.ImportedToCad);
        if (!batch.MaterialsChecked || !batch.PreviewChecked || !batch.MachineReadyChecked)
        {
            throw new BatchStateException("Final approval requires materials, preview, and machine-ready checklist items.");
        }

        batch.State = BatchState.OperatorApproved;
    }

    public static void EnsureState(Batch batch, BatchState expected)
    {
        if (batch.State != expected)
        {
            throw new BatchStateException($"Expected state {expected}, but current state is {batch.State}.");
        }
    }
}
