namespace EngravingStation.Core.Models;

public sealed class Batch
{
    private readonly List<BatchItem> _items = [];

    public BatchState State { get; set; } = BatchState.Draft;
    public IReadOnlyList<BatchItem> Items => _items;
    public BoardLayout? Layout { get; set; }
    public bool LayoutManuallyConfirmed { get; set; }
    public bool MaterialsChecked { get; set; }
    public bool PreviewChecked { get; set; }
    public bool MachineReadyChecked { get; set; }

    public void AddItem(BatchItem item) => _items.Add(item);
    public void Clear()
    {
        _items.Clear();
        Layout = null;
        LayoutManuallyConfirmed = false;
        MaterialsChecked = false;
        PreviewChecked = false;
        MachineReadyChecked = false;
        State = BatchState.Draft;
    }
}
