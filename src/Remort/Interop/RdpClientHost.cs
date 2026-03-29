using System.IO;
using System.Runtime.InteropServices;
using Remort.Connection;

namespace Remort.Interop;

/// <summary>
/// Hosts the Microsoft RDP Client ActiveX control (MsRdpClient10NotSafeForScripting)
/// using a manual AxHost subclass with proper COM event sinking.
/// Adapted from poc/StickyDesktop/RdpClientHost.cs.
/// </summary>
public sealed class RdpClientHost : AxHost, IRdpClient
{
    // CLSID for MsRdpClient10NotSafeForScripting
    private const string RdpClientClsid = "A0C63C30-F08D-4AB4-907C-34905D770C7D";

    private dynamic? _ocx;
    private EventSinkCookie? _sinkCookie;

    /// <summary>
    /// Initializes a new instance of the <see cref="RdpClientHost"/> class.
    /// </summary>
    public RdpClientHost()
        : base(RdpClientClsid)
    {
    }

    // --- Events raised by the COM control ---

    /// <inheritdoc/>
    public event EventHandler? Connecting;

    /// <inheritdoc/>
    public event EventHandler? Connected;

    /// <inheritdoc/>
    public event EventHandler<DisconnectedEventArgs>? Disconnected;

    /// <summary>Occurs when a non-fatal warning is received.</summary>
    public event EventHandler<WarningEventArgs>? Warning;

    /// <summary>Occurs when a fatal error is received.</summary>
    public event EventHandler<FatalErrorEventArgs>? FatalError;

    // --- Properties ---

    /// <inheritdoc/>
    public string Server
    {
        get => (string)(_ocx?.Server ?? string.Empty);
        set
        {
            if (_ocx != null)
            {
                _ocx.Server = value;
            }
        }
    }

    /// <inheritdoc/>
    public int DesktopWidth
    {
        get => (int)(_ocx?.DesktopWidth ?? 0);
        set
        {
            if (_ocx != null)
            {
                _ocx.DesktopWidth = value;
            }
        }
    }

    /// <inheritdoc/>
    public int DesktopHeight
    {
        get => (int)(_ocx?.DesktopHeight ?? 0);
        set
        {
            if (_ocx != null)
            {
                _ocx.DesktopHeight = value;
            }
        }
    }

    /// <inheritdoc/>
    public bool IsConnected => (_ocx?.Connected ?? 0) != 0;

    /// <inheritdoc/>
    public int ExtendedDisconnectReason
    {
        get
        {
            try
            {
                return (int)(_ocx?.ExtendedDisconnectReason ?? 0);
            }
            catch (COMException)
            {
                return -1;
            }
        }
    }

    // --- Methods ---

    /// <inheritdoc/>
    public string GetErrorDescription(int disconnectReason, int extendedReason)
    {
        try
        {
            return (string?)_ocx?.GetErrorDescription((uint)disconnectReason, (uint)extendedReason) ?? string.Empty;
        }
        catch (COMException)
        {
            return string.Empty;
        }
    }

    /// <inheritdoc/>
    public void ApplyDefaultSettings()
    {
        if (_ocx == null)
        {
            throw new InvalidOperationException(
                "Cannot apply settings: the RDP ActiveX control has not been initialized. " +
                "Ensure the control is sited in a WindowsFormsHost before calling this method.");
        }

        LogDiagnostic("ApplyDefaultSettings", $"OCX={_ocx != null}, IsHandleCreated={IsHandleCreated}, Width={Width}, Height={Height}, Server='{Server}'");

        dynamic ocx = _ocx!;
        dynamic advancedSettings = ocx.AdvancedSettings9;
        advancedSettings.EnableCredSspSupport = true;   // NLA
        advancedSettings.SmartSizing = true;
        advancedSettings.AuthenticationLevel = 2;       // Attempt authentication
        advancedSettings.RedirectSmartCards = true;      // Windows Hello / smart card credential redirect

        // ColorDepth is a top-level OCX property, not on AdvancedSettings
        ocx.ColorDepth = 32;

        LogDiagnostic("ApplyDefaultSettings", "OK");
    }

    /// <inheritdoc/>
    public void Connect()
    {
        LogDiagnostic("Connect", $"OCX={_ocx != null}, IsHandleCreated={IsHandleCreated}, Width={Width}, Height={Height}, Server='{Server}', DesktopWidth={DesktopWidth}, DesktopHeight={DesktopHeight}");
        _ocx?.Connect();
        LogDiagnostic("Connect", "Call returned");
    }

    /// <inheritdoc/>
    public void Disconnect()
    {
        if (IsConnected)
        {
            _ocx?.Disconnect();
        }
    }

    /// <summary>
    /// Captures the current RDP control content as a PNG screenshot.
    /// </summary>
    /// <param name="filePath">The full path to save the PNG file to.</param>
    public void CaptureScreenshot(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        string? directory = System.IO.Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        using var bitmap = new System.Drawing.Bitmap(Width, Height);
        DrawToBitmap(bitmap, new System.Drawing.Rectangle(0, 0, Width, Height));
        bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
    }

    internal void RaiseConnecting() => Connecting?.Invoke(this, EventArgs.Empty);

    internal void RaiseConnected() => Connected?.Invoke(this, EventArgs.Empty);

    internal void RaiseDisconnected(int reason) => Disconnected?.Invoke(this, new DisconnectedEventArgs(reason));

    internal void RaiseWarning(int code) => Warning?.Invoke(this, new WarningEventArgs(code));

    internal void RaiseFatalError(int code) => FatalError?.Invoke(this, new FatalErrorEventArgs(code));

    /// <inheritdoc/>
    protected override void AttachInterfaces()
    {
        _ocx = GetOcx();
    }

    /// <inheritdoc/>
    protected override void CreateSink()
    {
        object? ocx = GetOcx();
        if (ocx != null)
        {
            var sink = new MsTscAxEventsSink(this);
            _sinkCookie = new EventSinkCookie(ocx, sink, typeof(IMsTscAxEvents));
        }
    }

    /// <inheritdoc/>
    protected override void DetachSink()
    {
        _sinkCookie?.Disconnect();
        _sinkCookie = null;
    }

    private static void LogDiagnostic(string method, string message)
    {
        try
        {
            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Remort",
                "rdp-diag.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] {method}: {message}{Environment.NewLine}");
        }
#pragma warning disable CA1031 // Diagnostics must not throw
        catch (Exception)
#pragma warning restore CA1031
        {
            // If we can't log, we can't log.
        }
    }
}
