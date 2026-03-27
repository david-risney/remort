<!--
  Sync Impact Report
  Version change: 0.0.0 → 1.0.0 (initial ratification)
  Added principles: I–VII (all new)
  Added sections: Technology Constraints, Quality Gates, Development Workflow, Governance
  Templates requiring updates: ✅ plan-template.md (no changes needed — gates reference constitution)
  Follow-up TODOs: none
-->

# Remort Constitution

## Core Principles

### I. MVVM-First

All UI logic MUST live in ViewModels, not code-behind. Views (`.xaml.cs`) contain only `InitializeComponent()` and framework-mandated event wiring. ViewModels MUST NOT reference WPF types (`Window`, `MessageBox`, `Dispatcher`). Use service interfaces for UI interactions. CommunityToolkit.Mvvm source generators (`[ObservableProperty]`, `[RelayCommand]`) are the standard pattern.

### II. COM Interop Isolation

All MsRdpClient ActiveX and Win32 P/Invoke calls MUST be confined to dedicated interop files in the `Interop/` layer. No `dynamic` COM dispatch or `[DllImport]` may appear in ViewModels, Views, or service classes. The interop layer is the only code allowed to reference `AxHost`, `WindowsFormsHost`, or COM interfaces. See [ADR-002](docs/decisions/002-manual-axhost-com-interop.md) for rationale.

**No silent failures in interop code.** Methods that configure or operate on the COM control MUST throw `InvalidOperationException` if the underlying OCX is not initialized, rather than silently returning. Silent null guards mask lifecycle bugs that surface later as confusing, unrelated errors.

### III. Test-First

Tests MUST be written before or alongside implementation — never deferred. Every public service and ViewModel method MUST have corresponding unit tests. The test project (`src/Remort.Tests/`) uses xUnit, FluentAssertions, and NSubstitute. All tests MUST pass before a task is considered complete. No regressions allowed — existing tests MUST continue to pass.

### IV. Zero Warnings

`TreatWarningsAsErrors` is enabled project-wide. `AnalysisLevel=latest-all` activates the strictest Roslyn diagnostics. StyleCop.Analyzers enforce code style. No suppression pragmas (`#pragma warning disable`) without an accompanying code comment explaining why. The build MUST be clean — zero errors, zero warnings.

### V. Specification Before Code

Every feature MUST begin with a specification (`specs/###-feature-name/spec.md`) that defines WHAT and WHY in user-facing, technology-agnostic language. Implementation plans define HOW. Code is the last step, generated from executable task lists. No feature work begins without a spec. See [DEVELOPMENT.md](DEVELOPMENT.md) for the Spec Kit workflow.

### VI. Layered Dependencies

Code follows a strict layered architecture within each domain: Views → ViewModels → Services → Interop → Platform APIs. Each layer may only depend on the layer directly below it. No upward dependencies. No cross-domain dependencies except through shared interfaces. See [docs/architecture.md](docs/architecture.md) for the domain and layer map.

### VII. Simplicity

Start with the simplest implementation that satisfies the spec. No speculative features ("might need later"). No abstractions for single-use cases. Prefer framework-provided patterns over custom infrastructure. Complexity MUST be justified by a concrete requirement, not a hypothetical one.

## Technology Constraints

- **Runtime**: .NET 8 (`net8.0-windows`)
- **UI Framework**: WPF with WindowsFormsHost bridge for ActiveX hosting
- **MVVM Toolkit**: CommunityToolkit.Mvvm (source generators)
- **RDP Control**: MsRdpClient ActiveX via manual `AxHost` subclass — no `<COMReference>`
- **Testing**: xUnit, FluentAssertions, NSubstitute, Verify, coverlet
- **Analyzers**: StyleCop.Analyzers, Microsoft.CodeAnalysis.NetAnalyzers (`AnalysisLevel=latest-all`)
- **Code Style**: dotnet/runtime conventions — see [CODING-STYLE.md](CODING-STYLE.md)

## Quality Gates

| Gate | Requirement |
|------|-------------|
| Build | Zero errors, zero warnings (`TreatWarningsAsErrors`) |
| Tests | 100% pass rate (no skipped tests without justification) |
| Format | `dotnet format --verify-no-changes` passes |
| XAML | XamlStyler rules followed |
| Spec | Every feature has `spec.md` before implementation begins |
| Review | PRs reviewed before merge |

## Development Workflow

Features follow the Spec Kit pipeline:

```
constitution → specify → clarify → plan → checklist → tasks → analyze → implement
```

1. Specs define WHAT/WHY (technology-agnostic)
2. Plans define HOW (architecture, file paths, dependencies)
3. Tasks are atomic, ordered, and reference specific files
4. Implementation follows the task list, running tests at each step

Feature branches use the naming convention `###-short-name` (e.g., `001-rdp-connect`).

## Governance

This constitution supersedes ad-hoc conventions. All code contributions — human or agent — MUST comply with these principles. Amendments require:

1. Explicit documentation of rationale for the change
2. Version bump following semantic versioning (MAJOR for principle removal/redefinition, MINOR for additions, PATCH for clarifications)
3. Consistency check against templates in `.specify/templates/`

**Version**: 1.1.0 | **Ratified**: 2026-03-22 | **Last Amended**: 2026-03-23
