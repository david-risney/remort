# ADR-001: WPF over WinUI 3

## Status

Accepted

## Context

We need a UI framework for a Windows desktop app that hosts the MsRdpClient ActiveX control. The candidates were:

- **WPF** (.NET 8)
- **WinUI 3** (Windows App SDK)
- **WinForms** (.NET 8)

## Decision

Use **WPF**.

## Rationale

- The RDP ActiveX control requires `AxHost` (WinForms), which WPF can host via `WindowsFormsHost`. This is a supported, well-documented path.
- WinUI 3 would require XAML Islands to host WinForms content inside a WinUI window — adding a layer of complexity for no benefit.
- WPF offers a richer styling/theming model than WinForms, supporting the modern UI goals (color profiles, customizable themes).
- Pure WinForms would work for hosting but offers less flexibility for the UI layer.

## Consequences

- Must enable `<UseWindowsForms>true</UseWindowsForms>` alongside `<UseWPF>true</UseWPF>`.
- WindowsFormsHost has airspace limitations (the hosted control renders on top of WPF content). XAML overlays above the RDP viewport won't work — any overlay UI must be outside the host region.
