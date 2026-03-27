# Remort 👻

**Remort** is a modern Windows remote desktop client built with C# and WPF. It focuses on improving the experience over the official Remote Desktop apps with better virtual desktop integration, smarter reconnection, and a modern UI.

## Features

- **Virtual Desktop Pinning** — Pin a remote desktop connection to a specific virtual desktop
- **Easy Virtual Desktop Switching** — Quickly switch virtual desktops from the parent window
- **Limited Retries** — Caps connection retries instead of endlessly looping on failures
- **Smart Auto-Reconnect** — Configurable reconnection triggers:
  - On login
  - When visible on a virtual desktop
- **Flexible Connection Targets** — Supports both `devbox.microsoft.com` and explicit hostnames
- **Modern UI** — Customizable and preset color profiles for a polished look

## Contributing

See [DEVELOPMENT.md](DEVELOPMENT.md) for the development process, harness engineering approach, and Spec Kit workflow.

## Requirements

- Windows 10/11
- .NET 8.0

## Building

```
dotnet build
```

## License

See [LICENSE](LICENSE) for details.
