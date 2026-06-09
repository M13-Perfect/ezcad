# AGENTS.md

## Project

This repository contains a Windows desktop application for batch laser engraving job preparation.

The application is called EngravingStation.

It receives order codes from a barcode scanner, CSV, or Excel-like batch input, resolves matching asset files, generates a board layout preview, and imports the prepared layout into JCZ / EzCAD software.

## Language and UI

- Use C#.
- Use WPF for the desktop UI.
- Use MVVM.
- Use nullable reference types.
- Use async/await for IO.
- Keep business logic out of code-behind.
- XAML code-behind should only contain UI glue.

## Architecture

Projects:

- EngravingStation.App: WPF UI.
- EngravingStation.Core: domain models, validation, state machine, layout logic.
- EngravingStation.Infrastructure: SQLite, file system, scanner input, logging.
- EngravingStation.Cad: CAD adapter abstractions and implementations.
- tests: unit tests.

## Safety Rules

This is a production-adjacent engraving workflow.

Never automatically start engraving after scan or import.

The application must always require explicit manual confirmation before any operation that could affect a machine or production job.

For MVP, implement MockCadAdapter and LayoutFileCadAdapter first.
Do not invent JCZ SDK function signatures.
JczSdkCadAdapter must remain a stub until official SDK files, headers, or examples are added to the repository.

## Required Workflow

A batch must follow this state flow:

Draft -> Validating -> ReadyToLayout -> LayoutGenerated -> LayoutConfirmed -> ImportingToCad -> ImportedToCad -> OperatorApproved -> Completed.

Invalid states must be rejected with clear error messages.

## Error Handling

Every external operation must return a typed result or throw a domain-specific exception that is caught at the UI boundary.

Handle:

- empty scan input
- invalid order code
- duplicate order code
- order not found
- zero assets found
- multiple assets found
- missing file
- unsupported file type
- invalid dimensions
- layout overflow
- layout collision
- CAD adapter failure
- database failure

## Testing

Add unit tests for:

- order code normalization
- regex-based rule matching
- CSV import
- duplicate detection
- asset resolution
- layout slot assignment
- layout overflow
- batch state transitions
- confirmation guard rules

Run before final response:

dotnet restore
dotnet build
dotnet test

## Packaging

The app should publish for Windows.

Support both win-x64 and win-x86 publish profiles because EzCAD2 SDK may require x86 while EzCAD3 SDK may require x64.

## Definition of Done

A task is done only when:

- solution builds
- tests pass
- no business logic is in WPF code-behind
- unsafe machine operations are blocked behind manual confirmation
- README or docs are updated when behavior changes
