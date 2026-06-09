using EngravingStation.Core.Exceptions;
using EngravingStation.Core.Models;
using EngravingStation.Core.Results;
using EngravingStation.Core.State;
using Xunit;

namespace EngravingStation.Core.Tests;

public sealed class BatchStateMachineTests
{
    [Fact]
    public void MoveNext_FollowsRequiredStateFlow()
    {
        var batch = new Batch();
        var machine = new BatchStateMachine();
        machine.MoveNext(batch);
        Assert.Equal(BatchState.Validating, batch.State);
        machine.MoveNext(batch);
        Assert.Equal(BatchState.ReadyToLayout, batch.State);
    }

    [Fact]
    public void ConfirmLayout_RequiresLayoutGeneratedState()
    {
        var batch = new Batch { State = BatchState.ReadyToLayout };
        Assert.Throws<BatchStateException>(() => new BatchStateMachine().ConfirmLayout(batch));
    }

    [Fact]
    public void ImportToCad_RequiresManualConfirmationAndNoWarningsOrErrors()
    {
        var batch = new Batch { State = BatchState.LayoutGenerated };
        var machine = new BatchStateMachine();
        machine.ConfirmLayout(batch);
        var validation = new OperationResult();
        validation.AddWarning("WARN", "warning");
        Assert.Throws<BatchStateException>(() => machine.EnsureCanImportToCad(batch, validation));
    }

    [Fact]
    public void FinalOperatorApproval_RequiresChecklist()
    {
        var batch = new Batch { State = BatchState.ImportedToCad };
        Assert.Throws<BatchStateException>(() => new BatchStateMachine().FinalOperatorApproval(batch));
        batch.MaterialsChecked = true;
        batch.PreviewChecked = true;
        batch.MachineReadyChecked = true;
        new BatchStateMachine().FinalOperatorApproval(batch);
        Assert.Equal(BatchState.OperatorApproved, batch.State);
    }
}
