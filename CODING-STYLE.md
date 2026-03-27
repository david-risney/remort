# Coding Style

## Baseline

This project follows the [dotnet/runtime coding style](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md) as its baseline. The key rules from that guide are summarized below for quick reference, with Remort-specific additions in the sections that follow.

### dotnet/runtime Rules (Summary)

1. Use [Allman style](https://en.wikipedia.org/wiki/Indentation_style#Allman_style) braces, where each brace gets its own line.
2. Use four spaces of indentation (no tabs).
3. Use `_camelCase` for internal and private fields; prefix static fields with `s_`, thread-static fields with `t_`.
4. Avoid `this.` unless absolutely necessary.
5. Always specify visibility, even if it's the default (e.g., `private string _foo` not `string _foo`).
6. Namespace imports at the top of the file, sorted alphabetically, with `System` namespaces first.
7. Avoid more than one empty line at any time.
8. Avoid trailing whitespace.
9. Use language keywords instead of BCL types (e.g., `int`, `string`, `float` instead of `Int32`, `String`, `Single`).
10. Use `nameof(...)` instead of hard-coded strings when referring to symbols.
11. Fields should be specified at the top within type declarations.
12. Use `var` only when the type is apparent from the right side of the assignment.
13. Use PascalCase for all public members, types, and namespaces.

The project `.editorconfig` encodes these rules for automated enforcement. See the [dotnet/runtime `.editorconfig`](https://github.com/dotnet/runtime/blob/main/.editorconfig) for the reference configuration.

## Remort-Specific Conventions

### Naming

| Element | Convention | Example |
|---------|-----------|---------|
| ViewModels | Suffix with `ViewModel` | `ConnectionViewModel` |
| Views (Windows/UserControls) | Suffix with `Window`, `View`, or `Dialog` | `MainWindow`, `ConnectionView`, `SettingsDialog` |
| Dependency properties | Static field + property wrapper | `public static readonly DependencyProperty IsConnectedProperty` |
| Event handlers | `On` + event name | `OnConnectionStateChanged` |
| Commands | Suffix with `Command` | `ConnectCommand`, `DisconnectCommand` |
| Interfaces | Prefix with `I` | `IConnectionManager` |

### File Organization

- **One type per file.** File name matches the type name.
- **Views and ViewModels are co-located** in the same feature folder, not in separate `Views/` and `ViewModels/` trees.
- **XAML and code-behind** share the same file name (`ConnectionView.xaml` + `ConnectionView.xaml.cs`).
- **Minimal code-behind.** Logic belongs in the ViewModel or service layer, not in `.xaml.cs`.

### XAML

- Attributes on separate lines when an element has more than two attributes.
- `x:Name` before other attributes when present.
- Use `x:DataType` on `DataTemplate` for compile-time binding validation where supported.
- Bind to ViewModel properties — avoid `ElementName` bindings when a ViewModel binding is available.
- Use [XamlStyler](https://github.com/Xavalon/XamlStyler) default ordering (configured via `.xamlstyler` at the repo root).

### MVVM Patterns

- Use **CommunityToolkit.Mvvm** for `ObservableObject`, `RelayCommand`, and source generators (`[ObservableProperty]`, `[RelayCommand]`).
- ViewModels should not reference WPF types (`Window`, `MessageBox`, etc.). Use service interfaces for UI interactions.
- Prefer constructor injection for dependencies.

### Dependency Properties

```csharp
public static readonly DependencyProperty IsConnectedProperty =
    DependencyProperty.Register(
        nameof(IsConnected),
        typeof(bool),
        typeof(ConnectionHost),
        new PropertyMetadata(false, OnIsConnectedChanged));

public bool IsConnected
{
    get => (bool)GetValue(IsConnectedProperty);
    set => SetValue(IsConnectedProperty, value);
}

private static void OnIsConnectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    // ...
}
```

### Async / Await

- Suffix async methods with `Async` (e.g., `ConnectAsync`).
- Use `ConfigureAwait(false)` in library/service code that doesn't need the UI thread.
- Never use `async void` except for event handlers.
- Prefer `ValueTask` over `Task` for hot paths that frequently complete synchronously.

### Error Handling

- Use specific exception types — avoid catching `Exception` broadly.
- Never swallow exceptions silently. At minimum, log them.
- Validate public API inputs; trust internal calls.
- Use nullable reference types (`?`) and null checks at boundaries rather than defensive null checks everywhere.

### Comments

- Don't add comments that restate the code. Comment *why*, not *what*.
- Use `///` XML doc comments on public APIs.
- Use `// TODO:` for known incomplete work — these are tracked by analyzers.

## Enforcement

These rules are enforced through:

1. **`.editorconfig`** — formatting, naming, and analyzer severity rules
2. **Roslyn analyzers** — `Microsoft.CodeAnalysis.NetAnalyzers`, `StyleCop.Analyzers`
3. **`TreatWarningsAsErrors`** — analyzer violations break the build
4. **XamlStyler** — consistent XAML formatting
5. **Code review** — human and agent review for patterns not captured by rules
