# Development Process

This project uses **harness engineering** principles and **Spec Kit** to enable effective AI-assisted development. Rather than relying on ad-hoc prompting, we invest in the environment around the code — documentation, constraints, feedback loops, and tooling — so that any AI agent can be productive in this repo.

## What Is Harness Engineering?

Harness engineering is the practice of designing the scaffolding that keeps AI coding agents productive and on track. The key insight: when an agent produces poor output, the fix is almost never "try harder" — it's "what context was missing?" The harness is the part we own and compound over time, independent of any specific model or tool.

**References:**
- [Harness engineering: leveraging Codex in an agent-first world](https://openai.com/index/harness-engineering/) — OpenAI
- [Effective harnesses for long-running agents](https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents) — Anthropic
- [Building the Agent Harness](https://dev.to/skhandelwal/building-the-agent-harness-why-the-environment-matters-more-than-the-model-39ie/) — Shashank Khandelwal

## Five Layers of the Harness

| Layer | Purpose | Remort Implementation |
|-------|---------|----------------------|
| **Project Memory** | Entry-point docs that tell agents what this project is | `README.md`, `AGENTS.md`, `.github/copilot-instructions.md` |
| **Decision Records** | Capture *why* past decisions were made | `docs/decisions/` — lightweight ADRs |
| **Navigable Map** | Linked documentation agents can traverse | `docs/` with architecture, design, and feature docs cross-linked from `AGENTS.md` |
| **Workflow Automation** | Repeatable steps agents invoke instead of improvising | Spec Kit commands, PR Issues task, build scripts |
| **Enforcement** | Catches drift before human review | Linters, analyzers, CI gates, pre-commit checks |

## Spec Kit (Spec-Driven Development)

[Spec Kit](https://github.com/github/spec-kit) is GitHub's open-source toolkit for Spec-Driven Development (SDD). Specifications become the primary artifact — defining *what* and *why* before *how* — and AI agents generate implementations from them.

**Workflow:**

```
constitution → specify → clarify → plan → checklist → tasks → analyze → implement
```

Each feature follows this pipeline:

1. **Constitution** (`/speckit.constitution`) — Project governing principles: C# / WPF / .NET 8, testing standards, UI patterns, Win32 interop guidelines
2. **Specify** (`/speckit.specify`) — Define *what* a feature does and *why*, technology-agnostic (e.g., "Users can pin a connection to a virtual desktop")
3. **Clarify** (`/speckit.clarify`) — Resolve ambiguities with targeted questions
4. **Plan** (`/speckit.plan`) — Technical implementation plan: architecture, file structure, dependencies
5. **Checklist** (`/speckit.checklist`) — Domain-specific quality gates (security, accessibility, performance)
6. **Tasks** (`/speckit.tasks`) — Executable, dependency-ordered task list with user story priorities
7. **Analyze** (`/speckit.analyze`) — Cross-artifact consistency validation (spec ↔ plan ↔ tasks)
8. **Implement** (`/speckit.implement`) — Execute tasks following the plan, running tests at each step

**Project structure for specs:**

```
specs/
└── ###-feature-name/
    ├── spec.md          # User-focused specification (WHAT/WHY)
    ├── plan.md          # Technical implementation plan (HOW)
    ├── tasks.md         # Executable task list
    └── checklists/      # Quality validation checklists
```

## Planned Harness Artifacts

| Artifact | Status | Description |
|----------|--------|-------------|
| `README.md` | ✅ | Project overview and harness plan |
| `CODING-STYLE.md` | ✅ | Coding conventions — dotnet/runtime baseline + WPF/MVVM rules |
| `AGENTS.md` | ✅ | Agent entry point — map to docs, conventions, and repo structure |
| `.editorconfig` | ✅ | Machine-enforced naming, formatting, and analyzer severity rules |
| `.xamlstyler` | ✅ | XAML formatting configuration |
| `docs/architecture.md` | ✅ | Architecture overview with domain/layer map |
| `docs/decisions/` | ✅ | Architecture Decision Records (001-wpf-over-winui, 002-manual-axhost) |
| `src/Remort/` | ✅ | App project — TreatWarningsAsErrors, nullable, WarningLevel 9999 |
| `src/Remort.Tests/` | ✅ | Test project — xUnit, FluentAssertions, NSubstitute, Verify, coverlet |
| `Remort.sln` | ✅ | Solution file linking all projects |
| `.github/workflows/build.yml` | ✅ | CI — build, format check, test, coverage |
| `specs/` | ✅ | Feature specifications directory (ready for first spec) |
| `.github/copilot-instructions.md` | 🔲 | GitHub Copilot steering file (use `AGENTS.md` for now) |
| `.specify/memory/constitution.md` | ✅ | Spec Kit constitution — 7 principles governing all development |

## Principles

- **Treat friction as a harness gap.** When an agent goes off-track, ask "what context was missing?" and fix the environment.
- **Specs are non-technical.** Define *what* and *why*, not frameworks or libraries.
- **Plans are technical.** Architecture, file paths, dependencies, and trade-offs.
- **Tasks are executable.** Atomic, dependency-ordered, with file paths and test expectations.
- **Iterate.** The first version of every doc will be wrong. The value is in the feedback loop.

## C# / WPF Quality Tooling

These tools form the enforcement layer of the harness — catching errors at compile time, in the IDE, during build, and in CI before they reach humans.

### Compile-Time Error Prevention

| Tool | Purpose | Configuration |
|------|---------|---------------|
| **Nullable reference types** | Eliminate `NullReferenceException` at compile time | `<Nullable>enable</Nullable>` in `.csproj` |
| **TreatWarningsAsErrors** | Promote all warnings to build-breaking errors | `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` |
| **ImplicitUsings** | Reduce boilerplate, ensure consistent using directives | `<ImplicitUsings>enable</ImplicitUsings>` |
| **WarningLevel 9999** | Enable the latest and strictest compiler diagnostics | `<WarningLevel>9999</WarningLevel>` |

### Static Analysis

| Tool | Purpose | Configuration |
|------|---------|---------------|
| **Microsoft.CodeAnalysis.NetAnalyzers** | Built-in Roslyn analyzers for API usage, globalization, performance, security | Enabled by default in .NET 8; severity configured in `.editorconfig` |
| **StyleCop.Analyzers** | Enforce consistent code style and formatting | NuGet package + `.editorconfig` rules |
| **SonarAnalyzer.CSharp** | Deep analysis for bugs, code smells, security hotspots | NuGet package; optional SonarCloud CI integration |
| **CommunityToolkit.Mvvm analyzers** | Source generators + diagnostics for MVVM patterns | Comes with `CommunityToolkit.Mvvm` NuGet package |

### WPF-Specific

| Tool | Purpose | Configuration |
|------|---------|---------------|
| **XAML binding failures as errors** | Surface data binding failures in the Output window / diagnostics | `PresentationTraceSources.TraceLevel="High"` and `System.Diagnostics` trace listeners |
| **XamlStyler** | Consistent XAML formatting and attribute ordering | VS extension or `dotnet tool` + `.xamlstyler` config |
| **x:DataType / compiled bindings** | Catch binding path errors at compile time instead of runtime | Use `x:DataType` on `DataTemplate` and compiled binding expressions where supported |

### Testing

| Tool | Purpose | Configuration |
|------|---------|---------------|
| **xUnit** | Unit test framework | NuGet package; test projects target `net8.0-windows` |
| **FluentAssertions** | Readable assertion syntax that produces clear failure messages | NuGet package |
| **NSubstitute** | Mocking / substitution for interface-based dependencies | NuGet package |
| **Verify** | Snapshot testing for complex objects and UI state | NuGet package; `.verified.` files committed to repo |
| **coverlet** | Code coverage collection | NuGet package; integrated with `dotnet test --collect:"XPlat Code Coverage"` |

### EditorConfig

See [CODING-STYLE.md](CODING-STYLE.md) for the full set of conventions. A shared `.editorconfig` at the repo root enforces:

- Naming conventions (PascalCase for public members, `_camelCase` for private fields)
- Formatting rules (indentation, braces, spacing)
- Analyzer severity overrides (e.g., promote specific warnings to errors)
- Code style preferences (`var` usage, expression-bodied members, pattern matching)

### PR Issues Task

The workspace includes a **PR Issues** VS Code task that runs `Get-PullRequestIssues` / `Watch-PullRequestIssues` to surface build errors and analyzer warnings directly in the Problems panel during development.
