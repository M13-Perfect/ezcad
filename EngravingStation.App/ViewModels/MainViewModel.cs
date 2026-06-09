using System.Collections.ObjectModel;
using System.IO;
using EngravingStation.App.Commands;
using EngravingStation.Cad;
using EngravingStation.Core.Exceptions;
using EngravingStation.Core.Models;
using EngravingStation.Core.Results;
using EngravingStation.Core.Services;
using EngravingStation.Core.State;
using EngravingStation.Infrastructure;
using EngravingStation.Infrastructure.Csv;
using EngravingStation.Infrastructure.Repositories;
using Microsoft.Win32;

namespace EngravingStation.App.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly Batch _batch = new();
    private readonly OrderCodeNormalizer _normalizer = new();
    private readonly BatchStateMachine _stateMachine = new();
    private readonly FixedSlotLayoutService _layoutService = new();
    private readonly ICadAdapter _cadAdapter;
    private readonly CsvBatchImporter _csvBatchImporter = new();
    private readonly BatchValidator _batchValidator;
    private OperationResult _latestValidation = new();
    private decimal _boardWidthMm = BoardDefinition.Default.WidthMm;
    private decimal _boardHeightMm = BoardDefinition.Default.HeightMm;
    private decimal _boardMarginMm = BoardDefinition.Default.MarginMm;
    private decimal _slotWidthMm = BoardDefinition.Default.SlotWidthMm;
    private decimal _slotHeightMm = BoardDefinition.Default.SlotHeightMm;
    private int _slotColumns = BoardDefinition.Default.Columns;
    private int _slotRows = BoardDefinition.Default.Rows;
    private string _scanInput = string.Empty;
    private string _statusMessage = "Draft batch ready.";
    private string _detailMessage = "No item selected.";
    private BatchItemViewModel? _selectedItem;
    private bool _materialsChecked;
    private bool _previewChecked;
    private bool _machineReadyChecked;

    private MainViewModel(BatchValidator batchValidator, ICadAdapter cadAdapter)
    {
        _batchValidator = batchValidator;
        _cadAdapter = cadAdapter;
        ImportCsvCommand = new AsyncRelayCommand(ImportCsvAsync, () => CanImportCsv);
        AddScanCommand = new RelayCommand(AddScan, () => CanAddScan);
        ValidateBatchCommand = new AsyncRelayCommand(ValidateBatchAsync, () => CanValidateBatch);
        GenerateLayoutCommand = new RelayCommand(GenerateLayout, () => CanGenerateLayout);
        ConfirmLayoutCommand = new RelayCommand(ConfirmLayout, () => CanConfirmLayout);
        ImportToCadCommand = new AsyncRelayCommand(ImportToCadAsync, () => CanImportToCad);
        FinalOperatorApprovalCommand = new RelayCommand(FinalOperatorApproval, () => CanFinalOperatorApproval);
        ClearBatchCommand = new RelayCommand(ClearBatch);
    }

    public ObservableCollection<BatchItemViewModel> QueueItems { get; } = [];
    public ObservableCollection<PreviewSlotViewModel> PreviewSlots { get; } = [];
    public ObservableCollection<PreviewCellViewModel> PreviewCells { get; } = [];
    public ObservableCollection<IssueViewModel> ErrorItems { get; } = [];
    public BatchState State => _batch.State;
    public string StateDescription => State switch
    {
        BatchState.Draft => "Add scans or import a CSV, then validate the queue.",
        BatchState.Validating => "Validation is running against order rules and asset mappings.",
        BatchState.ReadyToLayout => "Validation passed. Generate a board layout preview.",
        BatchState.LayoutGenerated => "Review the preview and manually confirm the layout.",
        BatchState.LayoutConfirmed => "Layout is confirmed. CAD import is enabled.",
        BatchState.ImportingToCad => "CAD import is in progress.",
        BatchState.ImportedToCad => "CAD import is complete. Complete the final operator checklist.",
        BatchState.OperatorApproved => "Operator approval recorded.",
        BatchState.Completed => "Batch is complete. No machine start command was sent.",
        _ => string.Empty
    };
    public bool CanImportCsv => State == BatchState.Draft;
    public bool CanAddScan => State == BatchState.Draft;
    public bool CanValidateBatch => State == BatchState.Draft;
    public bool CanGenerateLayout => State == BatchState.ReadyToLayout;
    public bool CanConfirmLayout => State == BatchState.LayoutGenerated;
    public bool CanImportToCad => State == BatchState.LayoutConfirmed;
    public bool IsFinalChecklistEnabled => State == BatchState.ImportedToCad;
    public bool CanFinalOperatorApproval => IsFinalChecklistEnabled && MaterialsChecked && PreviewChecked && MachineReadyChecked;
    public bool CanEditBatch => State == BatchState.Draft;
    public bool CanEditBoard => State is BatchState.Draft or BatchState.ReadyToLayout;
    public string ScanInput { get => _scanInput; set { _scanInput = value; OnPropertyChanged(); } }
    public string StatusMessage { get => _statusMessage; private set { _statusMessage = value; OnPropertyChanged(); } }
    public string DetailMessage { get => _detailMessage; private set { _detailMessage = value; OnPropertyChanged(); } }
    public decimal BoardWidthMm { get => _boardWidthMm; set { _boardWidthMm = value; OnPropertyChanged(); OnPropertyChanged(nameof(PreviewCanvasWidth)); } }
    public decimal BoardHeightMm { get => _boardHeightMm; set { _boardHeightMm = value; OnPropertyChanged(); OnPropertyChanged(nameof(PreviewCanvasHeight)); } }
    public decimal BoardMarginMm { get => _boardMarginMm; set { _boardMarginMm = value; OnPropertyChanged(); } }
    public decimal SlotWidthMm { get => _slotWidthMm; set { _slotWidthMm = value; OnPropertyChanged(); } }
    public decimal SlotHeightMm { get => _slotHeightMm; set { _slotHeightMm = value; OnPropertyChanged(); } }
    public int SlotColumns { get => _slotColumns; set { _slotColumns = value; OnPropertyChanged(); } }
    public int SlotRows { get => _slotRows; set { _slotRows = value; OnPropertyChanged(); } }
    public double PreviewCanvasWidth => (double)(BoardWidthMm * PreviewSlotViewModel.Scale);
    public double PreviewCanvasHeight => (double)(BoardHeightMm * PreviewSlotViewModel.Scale);


    public BatchItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            _selectedItem = value;
            DetailMessage = value?.Detail ?? "No item selected.";
            OnPropertyChanged();
        }
    }

    public bool MaterialsChecked
    {
        get => _materialsChecked;
        set
        {
            _materialsChecked = value;
            _batch.MaterialsChecked = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanFinalOperatorApproval));
            RaiseCanExecuteChanged();
        }
    }

    public bool PreviewChecked
    {
        get => _previewChecked;
        set
        {
            _previewChecked = value;
            _batch.PreviewChecked = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanFinalOperatorApproval));
            RaiseCanExecuteChanged();
        }
    }

    public bool MachineReadyChecked
    {
        get => _machineReadyChecked;
        set
        {
            _machineReadyChecked = value;
            _batch.MachineReadyChecked = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanFinalOperatorApproval));
            RaiseCanExecuteChanged();
        }
    }

    public AsyncRelayCommand ImportCsvCommand { get; }
    public RelayCommand AddScanCommand { get; }
    public AsyncRelayCommand ValidateBatchCommand { get; }
    public RelayCommand GenerateLayoutCommand { get; }
    public RelayCommand ConfirmLayoutCommand { get; }
    public AsyncRelayCommand ImportToCadCommand { get; }
    public RelayCommand FinalOperatorApprovalCommand { get; }
    public RelayCommand ClearBatchCommand { get; }

    public static MainViewModel CreateDefault()
    {
        var root = AppContext.BaseDirectory;
        var samplePath = Path.Combine(root, "samples", "asset-mapping.csv");
        var records = File.Exists(samplePath)
            ? new CsvMappingImporter().ImportAsync(samplePath).GetAwaiter().GetResult()
            : Array.Empty<OrderAssetRecord>();
        var repository = new InMemoryOrderAssetRepository(records);
        var resolver = new AssetResolver(repository, new LocalFileSystem());
        var validator = new BatchValidator(OrderCodeValidator.Default, resolver);
        return new MainViewModel(validator, new LayoutFileCadAdapter(Path.Combine(root, "cad-output")));
    }

    private async Task ImportCsvAsync()
    {
        var dialog = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*" };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var rows = await _csvBatchImporter.ImportAsync(dialog.FileName).ConfigureAwait(true);
            ResetBatchCollections();
            foreach (var row in rows)
            {
                _batch.AddItem(row);
                QueueItems.Add(new BatchItemViewModel(row));
            }

            StatusMessage = $"Imported {rows.Count} CSV batch rows. Validate the batch before layout.";
            ClearIssues();
        }
        catch (CsvImportException exception)
        {
            SetError("CSV_IMPORT_FAILURE", exception.Message);
        }

        RaiseStateChanged();
    }

    private void AddScan()
    {
        AddBatchItem(ScanInput);
        ScanInput = string.Empty;
        StatusMessage = "Scan added. Validate the batch before layout.";
        ClearIssues();
    }

    private void AddBatchItem(string rawInput)
    {
        var item = new BatchItem(rawInput, _normalizer.Normalize(rawInput));
        _batch.AddItem(item);
        QueueItems.Add(new BatchItemViewModel(item));
        RaiseStateChanged();
    }

    private async Task ValidateBatchAsync()
    {
        try
        {
            _stateMachine.MoveNext(_batch);
            RaiseStateChanged();
            _latestValidation = await _batchValidator.ValidateAsync(_batch).ConfigureAwait(true);
            if (_latestValidation.Succeeded)
            {
                _stateMachine.MoveNext(_batch, _latestValidation);
            }
            else
            {
                _batch.State = BatchState.Draft;
            }
            RefreshQueue();
            SetIssues(_latestValidation.Issues);
            StatusMessage = _latestValidation.Succeeded ? "Batch is ready for layout." : "Validation failed. Review the error panel before continuing.";
        }
        catch (EngravingStationException exception)
        {
            SetError("VALIDATION_FAILURE", exception.Message);
        }
        finally
        {
            RaiseStateChanged();
        }
    }

    private void GenerateLayout()
    {
        try
        {
            BatchStateMachine.EnsureState(_batch, BatchState.ReadyToLayout);
            ClearIssues();
            var layoutResult = _layoutService.Generate(_batch, CreateConfiguredBoard());
            if (!layoutResult.Succeeded || layoutResult.Value is null)
            {
                SetIssues(layoutResult.Issues);
                StatusMessage = "Layout generation failed. Review the error panel before continuing.";
                return;
            }

            _batch.Layout = layoutResult.Value;
            _batch.LayoutManuallyConfirmed = false;
            _stateMachine.MoveNext(_batch);
            PreviewSlots.Clear();
            PreviewCells.Clear();
            foreach (var cell in layoutResult.Value.Cells)
            {
                PreviewCells.Add(new PreviewCellViewModel(cell));
            }

            foreach (var slot in layoutResult.Value.Slots)
            {
                PreviewSlots.Add(new PreviewSlotViewModel(slot));
            }

            RefreshQueue();
            StatusMessage = "Layout generated. Manual confirmation is required before CAD import.";
            ClearIssues();
        }
        catch (EngravingStationException exception)
        {
            SetError("LAYOUT_GENERATION_FAILURE", exception.Message);
        }

        RaiseStateChanged();
    }

    private BoardDefinition CreateConfiguredBoard()
    {
        return new BoardDefinition(BoardWidthMm, BoardHeightMm, SlotWidthMm, SlotHeightMm, SlotColumns, SlotRows, BoardMarginMm);
    }

    private void ConfirmLayout()
    {
        try
        {
            _stateMachine.ConfirmLayout(_batch);
            StatusMessage = "Layout manually confirmed. CAD import is enabled, but engraving is never started automatically.";
        }
        catch (BatchStateException exception)
        {
            SetError("LAYOUT_CONFIRMATION_BLOCKED", exception.Message);
        }

        RaiseStateChanged();
    }

    private async Task ImportToCadAsync()
    {
        try
        {
            _stateMachine.EnsureCanImportToCad(_batch, _latestValidation);
            if (_batch.Layout is null)
            {
                StatusMessage = "No layout exists to import.";
                return;
            }

            _stateMachine.MoveNext(_batch);
            RaiseStateChanged();
            var cadImportResult = await _cadAdapter.ImportLayoutAsync(_batch.Layout).ConfigureAwait(true);
            SetIssues(cadImportResult.Issues);
            if (cadImportResult.Succeeded)
            {
                _stateMachine.MoveNext(_batch);
                StatusMessage = "Layout imported to CAD files. Final operator checklist is required; engraving is not started.";
            }
            else
            {
                _batch.State = BatchState.LayoutConfirmed;
                StatusMessage = "CAD import failed. Review the error panel before retrying.";
            }
        }
        catch (EngravingStationException exception)
        {
            _batch.State = BatchState.LayoutConfirmed;
            SetError("CAD_IMPORT_FAILURE", exception.Message);
        }
        finally
        {
            RaiseStateChanged();
        }
    }

    private void FinalOperatorApproval()
    {
        try
        {
            _stateMachine.FinalOperatorApproval(_batch);
            _stateMachine.MoveNext(_batch);
            StatusMessage = "Operator approved and batch marked completed. No machine start command was sent.";
            ClearIssues();
        }
        catch (BatchStateException exception)
        {
            SetError("FINAL_APPROVAL_BLOCKED", exception.Message);
        }

        RaiseStateChanged();
    }

    private void ClearBatch()
    {
        ResetBatchCollections();
        MaterialsChecked = false;
        PreviewChecked = false;
        MachineReadyChecked = false;
        DetailMessage = "No item selected.";
        StatusMessage = "Batch cleared.";
        ClearIssues();
        RaiseStateChanged();
    }

    private void ResetBatchCollections()
    {
        _batch.Clear();
        _latestValidation = new OperationResult();
        QueueItems.Clear();
        PreviewSlots.Clear();
        PreviewCells.Clear();
        ErrorItems.Clear();
        SelectedItem = null;
    }

    private void SetIssues(IEnumerable<OperationIssue> issues)
    {
        ErrorItems.Clear();
        foreach (var issue in issues)
        {
            ErrorItems.Add(new IssueViewModel(issue));
        }
    }

    private void SetError(string code, string message)
    {
        var result = new OperationResult();
        result.AddError(code, message);
        SetIssues(result.Issues);
        StatusMessage = message;
    }

    private void ClearIssues()
    {
        ErrorItems.Clear();
    }

    private void RefreshQueue()
    {
        foreach (var item in QueueItems)
        {
            item.Refresh();
        }

        DetailMessage = SelectedItem?.Detail ?? DetailMessage;
    }

    private void RaiseStateChanged()
    {
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(StateDescription));
        OnPropertyChanged(nameof(CanImportCsv));
        OnPropertyChanged(nameof(CanAddScan));
        OnPropertyChanged(nameof(CanValidateBatch));
        OnPropertyChanged(nameof(CanGenerateLayout));
        OnPropertyChanged(nameof(CanConfirmLayout));
        OnPropertyChanged(nameof(CanImportToCad));
        OnPropertyChanged(nameof(IsFinalChecklistEnabled));
        OnPropertyChanged(nameof(CanFinalOperatorApproval));
        OnPropertyChanged(nameof(CanEditBatch));
        OnPropertyChanged(nameof(CanEditBoard));
        RaiseCanExecuteChanged();
    }

    private void RaiseCanExecuteChanged()
    {
        ImportCsvCommand.RaiseCanExecuteChanged();
        AddScanCommand.RaiseCanExecuteChanged();
        ValidateBatchCommand.RaiseCanExecuteChanged();
        GenerateLayoutCommand.RaiseCanExecuteChanged();
        ConfirmLayoutCommand.RaiseCanExecuteChanged();
        ImportToCadCommand.RaiseCanExecuteChanged();
        FinalOperatorApprovalCommand.RaiseCanExecuteChanged();
        ClearBatchCommand.RaiseCanExecuteChanged();
    }
}
