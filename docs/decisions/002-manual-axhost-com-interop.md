# ADR-002: Manual AxHost COM Interop (No COMReference)

## Status

Accepted

## Context

The MsRdpClient ActiveX control (`mstscax.dll`) needs to be hosted in the application. The standard .NET approach is to use `<COMReference>` in the `.csproj` to generate interop assemblies at build time.

## Decision

Use a **manual `AxHost` subclass** with `dynamic` dispatch for COM property access and explicit `[ComImport]` interfaces for event sinking. Do not use `<COMReference>`.

## Rationale

- `<COMReference>` requires the MSBuild `ResolveComReference` task, which is **not supported** on the .NET Core version of MSBuild (`dotnet build`). It only works with `msbuild.exe` from a Visual Studio installation.
- Even when using Visual Studio's MSBuild, the generated interop assemblies produce warnings about unmarshalable arguments (`UIParentWindowHandle`).
- The manual approach builds cleanly with `dotnet build`, requires no extra tooling, and gives full control over the COM interface declarations.
- `dynamic` dispatch for property access (server name, resolution, NLA settings) is simple and works reliably.
- Explicit `[ComImport]` + `[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]` is required for COM event sinking — `IReflect`-based approaches don't work in .NET 8+.

## Consequences

- COM event interfaces (`IMsTscAxEvents`) must be declared manually with correct `[DispId]` values from the type library.
- Property access is untyped (`dynamic`) — errors show up at runtime, not compile time. Mitigate by centralizing all COM access in the interop layer.
- See [POC-LEARNINGS.md](../../poc/StickyDesktop/POC-LEARNINGS.md) for the full set of gotchas discovered during prototyping.
