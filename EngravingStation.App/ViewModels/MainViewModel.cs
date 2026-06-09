using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
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
    private readonly CsvMappingImporter _csvImporter = new();
    private BatchValidator _batchValidator;
    private OperationResult _latestValidation = new();
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
        ImportCsvCommand = new AsyncRelayCommand(ImportCsvAsync, () => State == BatchState.Draft);
        AddScanCommand = new RelayCommand(AddScan, () => State == BatchState.Draft);
        ValidateBatchCommand = new AsyncRelayCommand(ValidateBatchAsync, () => State is BatchState.Draft or BatchState.Validating);
        GenerateLayoutCommand = new RelayCommand(GenerateLayout, () => State == BatchState.ReadyToLayout);
        ConfirmLayoutCommand = new RelayCommand(ConfirmLayout, () => State == BatchState.LayoutGenerated);
        ImportToCadCommand = new AsyncRelayCommand(ImportToCadAsync, () => State == BatchState.LayoutConfirmed);
        FinalOperatorApprovalCommand = new RelayCommand(FinalOperatorApproval, () => State == BatchState.ImportedToCad && MaterialsChecked && PreviewChecked && MachineReadyChecked);
        ClearBatchCommand = new RelayCommand(ClearBatch);
    }

    public ObservableCollection<BatchItemViewModel> QueueItems { get; } = [];
    public ObservableCollection<PreviewSlotViewModel> PreviewSlots { get; } = [];
    public BatchState State => _batch.State;
    public string ScanInput { get => _scanInput; set { _scanInput = value; OnPropertyChanged(); } }
    public string StatusMessage { get => _statusMessage; private set { _statusMessage = value; OnPropertyChanged(); } }
    public string DetailMessage { get => _detailMessage; private set { _detailMessage = value; OnPropertyChanged(); } }

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

    public bool MaterialsChecked { get => _materialsChecked; set { _materialsChecked = value; _batch.MaterialsChecked = value; OnPropertyChanged(); RaiseCanExecuteChanged(); } }
    public bool PreviewChecked { get => _previewChecked; set { _previewChecked = value; _batch.PreviewChecked = value; OnPropertyChanged(); RaiseCanExecuteChanged(); } }
    public bool MachineReadyChecked { get => _machineReadyChecked; set { _machineReadyChecked = value; _batch.MachineReadyChecked = value; OnPropertyChanged(); RaiseCanExecuteChanged(); } }

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

        var rows = await _csvImporter.ImportAsync(dialog.FileName).ConfigureAwait(true);
        var repository = new InMemoryOrderAssetRepository(rows);
        _batchValidator = new BatchValidator(OrderCodeValidator.Default, new AssetResolver(repository, new LocalFileSystem()));
        _batch.Clear();
        QueueItems.Clear();
        foreach (var row in rows)
        {
            AddBatchItem(row.OrderNo);
        }

        StatusMessage = $"Imported {rows.Count} CSV mapping rows as batch items.";
        RaiseStateChanged();
    }

    private void AddScan()
    {
        AddBatchItem(ScanInput);
        ScanInput = string.Empty;
        StatusMessage = "Scan added. Validate the batch before layout.";
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
            _batch.State = BatchState.Validating;
            RaiseStateChanged();
            _latestValidation = await _batchValidator.ValidateAsync(_batch).ConfigureAwait(true);
            _batch.State = _latestValidation.Succeeded ? BatchState.ReadyToLayout : BatchState.Draft;
            RefreshQueue();
            StatusMessage = _latestValidation.Succeeded ? "Batch is ready for layout." : string.Join(Environment.NewLine, _latestValidation.Issues.Select(issue => issue.Message));
        }
        catch (EngravingStationException exception)
        {
            StatusMessage = exception.Message;
        }
        finally
        {
            RaiseStateChanged();
        }
    }

    private void GenerateLayout()
    {
        var layoutResult = _layoutService.Generate(_batch, BoardDefinition.Default);
        if (!layoutResult.Succeeded || layoutResult.Value is null)
        {
            StatusMessage = string.Join(Environment.NewLine, layoutResult.Issues.Select(issue => issue.Message));
            return;
        }

        _batch.Layout = layoutResult.Value;
        _batch.State = BatchState.LayoutGenerated;
        PreviewSlots.Clear();
        foreach (var slot in layoutResult.Value.Slots)
        {
            PreviewSlots.Add(new PreviewSlotViewModel(slot));
        }

        RefreshQueue();
        StatusMessage = "Layout generated. Manual confirmation is required before CAD import.";
        RaiseStateChanged();
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
            StatusMessage = exception.Message;
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

            _batch.State = BatchState.ImportingToCad;
            RaiseStateChanged();
            var result = await _cadAdapter.ImportLayoutAsync(_batch.Layout).ConfigureAwait(true);
            _batch.State = result.Succeeded ? BatchState.ImportedToCad : BatchState.LayoutConfirmed;
            StatusMessage = result.Succeeded ? "Layout imported to CAD files. Final operator checklist is required; engraving is not started." : string.Join(Environment.NewLine, result.Issues.Select(issue => issue.Message));
        }
        catch (EngravingStationException exception)
        {
            _batch.State = BatchState.LayoutConfirmed;
            StatusMessage = exception.Message;
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
            _batch.State = BatchState.Completed;
            StatusMessage = "Operator approved and batch marked completed. No machine start command was sent.";
        }
        catch (BatchStateException exception)
        {
            StatusMessage = exception.Message;
        }

        RaiseStateChanged();
    }

    private void ClearBatch()
    {
        _batch.Clear();
        QueueItems.Clear();
        PreviewSlots.Clear();
        MaterialsChecked = false;
        PreviewChecked = false;
        MachineReadyChecked = false;
        StatusMessage = "Batch cleared.";
        RaiseStateChanged();
    }

    private void RefreshQueue()
    {
        foreach (var item in QueueItems)
        {
            item.Refresh();
        }
    }

    private void RaiseStateChanged()
    {
        OnPropertyChanged(nameof(State));
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
