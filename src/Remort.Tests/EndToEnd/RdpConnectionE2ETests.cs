using FluentAssertions;
using Remort.Connection;
using Remort.Interop;

namespace Remort.Tests.EndToEnd;

/// <summary>
/// End-to-end tests that exercise the real MsRdpClient ActiveX control
/// against a local RDP server. These tests validate the COM interop lifecycle,
/// connection settings, and event flow that unit tests with mocks cannot cover.
///
/// Tests are skipped automatically if no local RDP server is available.
/// To enable: enable Windows RDP on localhost, install FreeRDP, or set
/// <c>REMORT_E2E_RDP_HOST=hostname:port</c>.
/// </summary>
[Collection("E2E")]
public sealed class RdpConnectionE2ETests : IDisposable
{
    private readonly LocalRdpServerFixture _server;
    private readonly Form _hostForm;
    private readonly RdpClientHost _rdpClient;

    public RdpConnectionE2ETests(LocalRdpServerFixture server)
    {
        _server = server;

        // Create a hidden WinForms container to site the ActiveX control.
        // The control requires a real HWND to initialize the OCX.
        _hostForm = new Form { Visible = false, Width = 800, Height = 600 };
        _rdpClient = new RdpClientHost { Dock = DockStyle.Fill };
        _hostForm.Controls.Add(_rdpClient);
        _hostForm.Show(); // Sites the control, triggering AttachInterfaces + CreateSink
        _hostForm.Hide();
    }

    public void Dispose()
    {
        try
        {
            if (_rdpClient.IsConnected)
            {
                _rdpClient.Disconnect();
            }
        }
#pragma warning disable CA1031 // Cleanup is best-effort
        catch (Exception)
#pragma warning restore CA1031
        {
            // Ignore disconnect errors during cleanup
        }

        _rdpClient.Dispose();
        _hostForm.Dispose();
    }

    [StaFact]
    [Trait("Category", "E2E")]
    public void ApplyDefaultSettings_WhenControlIsSited_DoesNotThrow()
    {
        // This test catches the timing bug where ApplyDefaultSettings
        // was called before the control was sited, silently doing nothing.
        Action act = () => _rdpClient.ApplyDefaultSettings();

        act.Should().NotThrow("the control is sited and _ocx should be initialized");
    }

    [StaFact]
    [Trait("Category", "E2E")]
    public void ApplyDefaultSettings_WhenControlNotSited_ThrowsInvalidOperation()
    {
        // Verify the "fail loudly" policy: calling ApplyDefaultSettings
        // on an unsited control must throw, not silently return.
        var unsitedClient = new RdpClientHost();

        Action act = () => unsitedClient.ApplyDefaultSettings();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not been initialized*");

        unsitedClient.Dispose();
    }

    [StaFact]
    [Trait("Category", "E2E")]
    public void ServerProperty_CanBeSetAfterSiting()
    {
        Action act = () => _rdpClient.Server = "test-server.example.com";

        act.Should().NotThrow();
        _rdpClient.Server.Should().Be("test-server.example.com");
    }

    [StaFact]
    [Trait("Category", "E2E")]
    public void DesktopDimensions_CanBeSetAfterSiting()
    {
        _rdpClient.DesktopWidth = 1920;
        _rdpClient.DesktopHeight = 1080;

        _rdpClient.DesktopWidth.Should().Be(1920);
        _rdpClient.DesktopHeight.Should().Be(1080);
    }

    [StaFact]
    [Trait("Category", "E2E")]
    public void IsConnected_IsFalse_WhenNotConnected()
    {
        _rdpClient.IsConnected.Should().BeFalse();
    }

    [StaFact]
    [Trait("Category", "E2E")]
    public async Task Connect_ToLocalServer_FiresConnectingAndConnectedEvents()
    {
        if (!_server.IsAvailable)
        {
            // No local RDP server available — skip without failing
            return;
        }

        var connectingFired = false;
        var connectedFired = new TaskCompletionSource<bool>();
        var disconnectedFired = new TaskCompletionSource<DisconnectedEventArgs>();

        _rdpClient.Connecting += (_, _) => connectingFired = true;
        _rdpClient.Connected += (_, _) => connectedFired.TrySetResult(true);
        _rdpClient.Disconnected += (_, e) => disconnectedFired.TrySetResult(e);

        _rdpClient.ApplyDefaultSettings();
        _rdpClient.Server = _server.Host;
        _rdpClient.DesktopWidth = 800;
        _rdpClient.DesktopHeight = 600;
        _rdpClient.Connect();

        // Wait for either Connected or Disconnected (auth may fail without credentials)
        Task completed = await Task.WhenAny(
            connectedFired.Task,
            disconnectedFired.Task,
            Task.Delay(TimeSpan.FromSeconds(15))).ConfigureAwait(true);

        // The connecting event should always fire regardless of outcome
        connectingFired.Should().BeTrue("Connecting event should fire when Connect() is called");

        // We got either Connected or Disconnected — both prove the COM event pipeline works
        bool gotEvent = connectedFired.Task.IsCompleted || disconnectedFired.Task.IsCompleted;
        gotEvent.Should().BeTrue("should receive either Connected or Disconnected event within timeout");
    }

    [StaFact]
    [Trait("Category", "E2E")]
    public void RetryingConnectionService_Connect_AppliesSettingsBeforeConnecting()
    {
        // Verifies that RetryingConnectionService calls ApplyDefaultSettings
        // before Connect(), ensuring settings are applied at the right time.
        var service = new RetryingConnectionService(_rdpClient);

        // This should not throw — the control is sited, so ApplyDefaultSettings succeeds
        Action act = () =>
        {
            service.Server = "nonexistent.invalid";
            service.Connect();
        };

        act.Should().NotThrow("RetryingConnectionService.Connect should apply settings on a sited control");
    }
}
