# EngravingStation

EngravingStation is an MVP Windows desktop application for batch laser engraving job preparation. It accepts scanner-like input, CSV batch rows, and CSV asset mapping data, validates order codes, resolves matching artwork assets, generates a fixed-slot board layout preview, and exports CAD handoff files. It does **not** control or start a real laser engraving machine.

## Documentation

- Requirements and MVP workflow details are maintained in `docs/REQUIREMENTS.md`.

## Projects

- `EngravingStation.App` - WPF MVVM desktop UI targeting `net10.0-windows`.
- `EngravingStation.Core` - domain models, validation, duplicate detection, layout logic, and batch state machine.
- `EngravingStation.Infrastructure` - CSV import, SQLite repository abstraction implementation, local file-system adapter, and in-memory repository.
- `EngravingStation.Cad` - CAD adapter abstraction, mock adapter, layout-file adapter, and an intentionally unimplemented JCZ SDK stub.
- `EngravingStation.Core.Tests` - unit tests for parser/import, resolver, layout, state machine, and duplicate detection.

## Safety model

The MVP is production-adjacent but deliberately safe:

1. Scanning and validation never start engraving.
2. Layout import to CAD requires an explicit **Confirm Layout** action.
3. CAD import is blocked when validation has warnings or errors.
4. Multiple matching assets are rejected; the app never guesses.
5. Final approval requires the operator checklist: materials checked, preview checked, and machine readiness checked.
6. `JczSdkCadAdapter` is a stub until official JCZ SDK files, headers, or examples are committed.

Required batch state flow:

```text
Draft -> Validating -> ReadyToLayout -> LayoutGenerated -> LayoutConfirmed -> ImportingToCad -> ImportedToCad -> OperatorApproved -> Completed
```


## Scanner input and order-code rules

Scanner input is normalized before validation by trimming surrounding whitespace, removing scanner control characters such as CR/LF terminators, converting Unicode compatibility characters such as full-width letters and digits to ASCII, and uppercasing with invariant culture. Empty or whitespace-only scans remain empty and are rejected during validation.

Order-code validation uses configurable regular-expression rules. The default MVP rules accept `ORD-` followed by at least four uppercase letters or digits, and `TRK` followed by at least six digits. Additional deployments can construct `OrderCodeValidator` with their own named `OrderCodeRule` entries without changing scan normalization.

## CSV batch and mapping formats

Batch CSV files contain one order code per row using this header:

```csv
order_code
```

Sample batch files are included under `samples/batches/`, including a valid batch and a duplicate-code example. The UI **Import CSV** action imports batch rows and keeps asset resolution backed by the configured asset mapping repository.

The MVP mapping CSV uses this header:

```csv
order_no,tracking_no,asset_path,width_mm,height_mm,version
```

A sample file is included at `samples/asset-mapping.csv` with sample SVG assets under `samples/assets/`. Additional mapping examples under `samples/mappings/` exercise missing-file, unsupported-extension, and multiple-match validation paths.


## Fixed-slot layout configuration

The layout engine uses a configurable `BoardDefinition` with board width, board height, margin, slot width, slot height, column count, and row count in millimeters. The WPF board preview exposes these values before **Generate Layout**, and `FixedSlotLayoutService.Generate` accepts the same definition for tests or alternate deployments.

Layout generation assigns valid assets to fixed slots in row-major order, centers each asset within its slot, and rejects invalid dimensions, grids that do not fit inside the configured board margin, batches that exceed slot capacity, assets that exceed a slot, and any detected item collisions. Preview data includes both slot-cell bounds and placed asset bounds so WPF can render the fixed grid and the actual asset footprint.

For debugging, `LayoutFileCadAdapter` writes `layout-preview.svg` using the shared SVG renderer. The SVG shows the board, usable margin area, fixed slot cells, and placed asset rectangles; it remains a handoff/debug artifact and never starts engraving.

## Layout-file CAD handoff

`LayoutFileCadAdapter` writes the CAD handoff to an output directory:

- `job.json` - serialized board layout and slots.
- `layout-preview.svg` - simple SVG preview of the fixed-slot layout.

These files are only a preparation/import handoff. They are not machine-start commands.

## Build and test

Install the .NET 10 SDK, then run:

```bash
dotnet restore EngravingStation.sln
dotnet build EngravingStation.sln
dotnet test EngravingStation.sln
```

## Windows publishing

Publish x64:

```bash
dotnet publish EngravingStation.App/EngravingStation.App.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true
```

Publish x86:

```bash
dotnet publish EngravingStation.App/EngravingStation.App.csproj --configuration Release --runtime win-x86 --self-contained true -p:PublishSingleFile=true
```

