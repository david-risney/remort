using FluentAssertions;
using NSubstitute;
using Remort.Connection;
using Remort.Settings;
using Remort.VirtualDesktop;

namespace Remort.Tests.VirtualDesktop;

/// <summary>
/// Unit tests for the reconnect-on-desktop-switch behavior in <see cref="MainWindowViewModel"/>.
/// </summary>
public class ReconnectOnDesktopSwitchTests
{
    private readonly IConnectionService _connectionService;
    private readonly ISettingsStore _settingsStore;
    private readonly IVirtualDesktopService _virtualDesktopService;

    public ReconnectOnDesktopSwitchTests()
    {
        _connectionService = Substitute.For<IConnectionService>();
        _settingsStore = Substitute.For<ISettingsStore>();
        _settingsStore.Load().Returns(new AppSettings());
        _virtualDesktopService = Substitute.For<IVirtualDesktopService>();
        _virtualDesktopService.IsSupported.Returns(true);
    }

    [Fact]
    public void TryReconnectOnDesktopSwitch_WhenAllPreconditionsMet_InitiatesReconnect()
    {
        _settingsStore.Load().Returns(new AppSettings
        {
            ReconnectOnDesktopSwitchEnabled = true,
            PinToDesktopEnabled = true,
            LastConnectedHost = "myserver.contoso.com",
        });
        MainWindowViewModel sut = CreateViewModel();

        sut.TryReconnectOnDesktopSwitch();

        _connectionService.Received(1).Connect();
        sut.ConnectionState.Should().Be(ConnectionState.Connecting);
        sut.Hostname.Should().Be("myserver.contoso.com");
    }

    [Fact]
    public void TryReconnectOnDesktopSwitch_WhenFeatureDisabled_DoesNotReconnect()
    {
        _settingsStore.Load().Returns(new AppSettings
        {
            ReconnectOnDesktopSwitchEnabled = false,
            PinToDesktopEnabled = true,
            LastConnectedHost = "myserver.contoso.com",
        });
        MainWindowViewModel sut = CreateViewModel();

        sut.TryReconnectOnDesktopSwitch();

        _connectionService.DidNotReceive().Connect();
        sut.ConnectionState.Should().Be(ConnectionState.Disconnected);
    }

    [Fact]
    public void TryReconnectOnDesktopSwitch_WhenPinDisabled_DoesNotReconnect()
    {
        _settingsStore.Load().Returns(new AppSettings
        {
            ReconnectOnDesktopSwitchEnabled = true,
            PinToDesktopEnabled = false,
            LastConnectedHost = "myserver.contoso.com",
        });
        MainWindowViewModel sut = CreateViewModel();

        sut.TryReconnectOnDesktopSwitch();

        _connectionService.DidNotReceive().Connect();
    }

    [Fact]
    public void TryReconnectOnDesktopSwitch_WhenAlreadyConnected_DoesNotReconnect()
    {
        _settingsStore.Load().Returns(new AppSettings
        {
            ReconnectOnDesktopSwitchEnabled = true,
            PinToDesktopEnabled = true,
            LastConnectedHost = "myserver.contoso.com",
        });
        MainWindowViewModel sut = CreateViewModel();

        // Simulate an active connection.
        sut.Hostname = "myserver.contoso.com";
        sut.ConnectCommand.Execute(null);
        _connectionService.Connected += Raise.Event();

        sut.TryReconnectOnDesktopSwitch();

        // Connect was called once for the manual connect, not again for desktop switch.
        _connectionService.Received(1).Connect();
    }

    [Fact]
    public void TryReconnectOnDesktopSwitch_WhenAlreadyConnecting_DoesNotReconnect()
    {
        _settingsStore.Load().Returns(new AppSettings
        {
            ReconnectOnDesktopSwitchEnabled = true,
            PinToDesktopEnabled = true,
            LastConnectedHost = "myserver.contoso.com",
        });
        MainWindowViewModel sut = CreateViewModel();

        // Simulate a connection in progress.
        sut.Hostname = "myserver.contoso.com";
        sut.ConnectCommand.Execute(null);

        sut.TryReconnectOnDesktopSwitch();

        // Connect was called once for the manual connect, not again for desktop switch.
        _connectionService.Received(1).Connect();
    }

    [Fact]
    public void TryReconnectOnDesktopSwitch_WhenNoLastHost_DoesNotReconnect()
    {
        _settingsStore.Load().Returns(new AppSettings
        {
            ReconnectOnDesktopSwitchEnabled = true,
            PinToDesktopEnabled = true,
            LastConnectedHost = string.Empty,
        });
        MainWindowViewModel sut = CreateViewModel();

        sut.TryReconnectOnDesktopSwitch();

        _connectionService.DidNotReceive().Connect();
    }

    [Fact]
    public void TryReconnectOnDesktopSwitch_SetsAutoReconnectFlag_ForStatusText()
    {
        _settingsStore.Load().Returns(new AppSettings
        {
            ReconnectOnDesktopSwitchEnabled = true,
            PinToDesktopEnabled = true,
            LastConnectedHost = "myserver.contoso.com",
        });
        MainWindowViewModel sut = CreateViewModel();

        sut.TryReconnectOnDesktopSwitch();

        // Simulate AttemptStarted to verify auto-reconnect status text.
        _connectionService.AttemptStarted += Raise.EventWith(
            new AttemptStartedEventArgs(1, 3));

        sut.StatusText.Should().Contain("Auto-reconnecting");
        sut.StatusText.Should().Contain("myserver.contoso.com");
    }

    [Fact]
    public void TryReconnectOnDesktopSwitch_OnRetriesExhausted_ShowsFailureMessage()
    {
        _settingsStore.Load().Returns(new AppSettings
        {
            ReconnectOnDesktopSwitchEnabled = true,
            PinToDesktopEnabled = true,
            LastConnectedHost = "myserver.contoso.com",
        });
        MainWindowViewModel sut = CreateViewModel();

        sut.TryReconnectOnDesktopSwitch();

        _connectionService.RetriesExhausted += Raise.EventWith(
            new RetriesExhaustedEventArgs(3, "Connection timed out"));

        sut.StatusText.Should().Contain("Auto-reconnect failed");
        sut.StatusText.Should().Contain("Connection timed out");
        sut.ConnectionState.Should().Be(ConnectionState.Disconnected);
    }

    [Fact]
    public void TryReconnectOnDesktopSwitch_CoexistsWithAutoReconnect_FirstOneWins()
    {
        _settingsStore.Load().Returns(new AppSettings
        {
            AutoReconnectEnabled = true,
            ReconnectOnDesktopSwitchEnabled = true,
            PinToDesktopEnabled = true,
            LastConnectedHost = "myserver.contoso.com",
        });
        MainWindowViewModel sut = CreateViewModel();

        // First: auto-reconnect claims the connection.
        sut.TryAutoReconnect();
        sut.ConnectionState.Should().Be(ConnectionState.Connecting);

        // Second: desktop switch finds Connecting state and does nothing.
        sut.TryReconnectOnDesktopSwitch();

        _connectionService.Received(1).Connect();
    }

    [Fact]
    public void ReconnectOnDesktopSwitchEnabled_DefaultsToFalse()
    {
        MainWindowViewModel sut = CreateViewModel();

        sut.ReconnectOnDesktopSwitchEnabled.Should().BeFalse();
    }

    [Fact]
    public void ReconnectOnDesktopSwitchEnabled_LoadedFromSettings()
    {
        _settingsStore.Load().Returns(new AppSettings { ReconnectOnDesktopSwitchEnabled = true });
        MainWindowViewModel sut = CreateViewModel();

        sut.ReconnectOnDesktopSwitchEnabled.Should().BeTrue();
    }

    [Fact]
    public void ReconnectOnDesktopSwitchEnabled_WhenChanged_PersistsSetting()
    {
        MainWindowViewModel sut = CreateViewModel();

        sut.ReconnectOnDesktopSwitchEnabled = true;

        _settingsStore.Received().Save(Arg.Is<AppSettings>(s => s.ReconnectOnDesktopSwitchEnabled == true));
    }

    private MainWindowViewModel CreateViewModel() =>
        new(_connectionService, settingsStore: _settingsStore, virtualDesktopService: _virtualDesktopService);
}
