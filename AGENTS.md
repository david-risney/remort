# Project Guidelines

## Overview

Remort is a C# WPF remote desktop client for Windows. See [README.md](README.md) for features and user-facing docs.

## Architecture

- **UI Framework**: WPF (.NET 8, `net8.0-windows`) with WindowsFormsHost bridge for ActiveX hosting
- **RDP Control**: MsRdpClient ActiveX via manual `AxHost` subclass — no `<COMReference>`, see [ADR-002](docs/decisions/002-manual-axhost-com-interop.md)
- **Pattern**: MVVM using CommunityToolkit.Mvvm (source generators, `[ObservableProperty]`, `[RelayCommand]`)
- **Project structure**: `src/Remort/` (app), `src/Remort.Tests/` (xUnit tests)
- See [docs/architecture.md](docs/architecture.md) for the full domain/layer map

## Code Style

Follow [CODING-STYLE.md](CODING-STYLE.md) — based on [dotnet/runtime conventions](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md) with WPF-specific additions.

Key rules: file-scoped namespaces, `_camelCase` private fields, `var` only when type is apparent, Allman braces, `public` visibility always explicit.

## Build and Test

```
dotnet build Remort.sln
dotnet test Remort.sln
```

All warnings are errors (`TreatWarningsAsErrors`). Analyzers: StyleCop, NetAnalyzers, `AnalysisLevel=latest-all`.

## Conventions

- **Views and ViewModels** live in the same feature folder, not in separate trees
- **Minimal code-behind** — logic goes in ViewModels or services, not `.xaml.cs`
- **COM interop** stays in dedicated files — don't scatter `dynamic` COM calls
- **Async methods** end with `Async`, use `ConfigureAwait(false)` in non-UI code, never `async void` except event handlers
- **Specs** live in `specs/###-feature-name/` — see [DEVELOPMENT.md](DEVELOPMENT.md) for the Spec Kit workflow

## Key Docs

| Doc | Purpose |
|-----|---------|
| [DEVELOPMENT.md](DEVELOPMENT.md) | Harness engineering, Spec Kit workflow, quality tooling |
| [CODING-STYLE.md](CODING-STYLE.md) | Naming, formatting, XAML, MVVM patterns |
| [docs/architecture.md](docs/architecture.md) | Domain map, layer diagram, dependency rules |
| [docs/decisions/](docs/decisions/) | Architecture Decision Records |
| [poc/StickyDesktop/POC-LEARNINGS.md](poc/StickyDesktop/POC-LEARNINGS.md) | COM interop findings from the proof of concept |
