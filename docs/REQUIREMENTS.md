# EngravingStation Requirements

## Purpose

EngravingStation is a Windows desktop application for preparing batch laser engraving jobs before they are handed off to JCZ / EzCAD software. The MVP focuses on safe job preparation: receiving order codes, resolving artwork assets, generating a previewable board layout, and exporting/importing layout files without ever starting engraving automatically.

## Solution Structure

The solution is split into focused projects:

- `EngravingStation.App` - WPF shell and MVVM presentation layer.
- `EngravingStation.Core` - domain models, validation, layout, and batch state transitions.
- `EngravingStation.Infrastructure` - CSV import, file-system access, and persistence-oriented adapters.
- `EngravingStation.Cad` - CAD handoff abstractions and safe MVP adapter implementations.
- `EngravingStation.Core.Tests` - unit tests for core and infrastructure behavior.

## MVP Workflow

A batch must move through the following states in order:

```text
Draft -> Validating -> ReadyToLayout -> LayoutGenerated -> LayoutConfirmed -> ImportingToCad -> ImportedToCad -> OperatorApproved -> Completed
```

Invalid transitions must be rejected with clear messages. Machine-affecting operations must remain blocked until the required manual confirmation state has been reached.

## Main Window Shell

The initial WPF shell launches into an empty `Draft` batch with the planned layout areas visible:

1. Header/status bar with the application name, current batch state, and status message.
2. Left batch queue pane for scanned or CSV-imported order codes.
3. Center board preview pane for generated layout slots.
4. Right details/errors pane with final operator approval checklist.
5. Bottom command bar for CSV import, scan entry, validation, layout generation, layout confirmation, CAD import, final approval, and clearing the batch.

No sample batch items should be loaded into the queue automatically on launch. Sample data may be available to support validation and demos, but the operator must explicitly import or scan work into the active batch.

## Safety Requirements

- Scanning an order code must never start engraving.
- Importing a layout to CAD must never start engraving.
- Layout import to CAD requires explicit manual layout confirmation.
- Final completion requires an explicit operator checklist.
- `JczSdkCadAdapter` must remain a stub until official JCZ SDK documentation, headers, or examples are added to this repository.
- The MVP should prefer `MockCadAdapter` and `LayoutFileCadAdapter` for safe development and testing.

## Validation and Error Handling

The system must handle these conditions with typed results or domain-specific exceptions caught at the UI boundary:

- Empty scan input.
- Invalid order code.
- Duplicate order code.
- Order not found.
- Zero assets found.
- Multiple assets found.
- Missing file.
- Unsupported file type.
- Invalid dimensions.
- Layout overflow.
- Layout collision.
- CAD adapter failure.
- Database failure.

## Packaging Requirements

The app must publish for Windows and support both runtimes:

- `win-x64` for modern EzCAD3-oriented deployments.
- `win-x86` for EzCAD2 SDK scenarios that may require a 32-bit process.

## Test Requirements

Before delivery, run these checks when the .NET SDK is available:

```bash
dotnet restore EngravingStation.sln
dotnet build EngravingStation.sln
dotnet test EngravingStation.sln
```

Unit coverage should include order code normalization, regex rule matching, CSV import, duplicate detection, asset resolution, layout slot assignment, layout overflow, batch state transitions, and confirmation guard rules.
