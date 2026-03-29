using FluentAssertions;
using NSubstitute;
using Remort.Connection;
using Remort.Devices;
using Remort.Interop;

namespace Remort.Tests.DeviceWindow;

/// <summary>
/// Tests for <see cref="Remort.DeviceWindow.ConnectionPageViewModel"/>.
/// </summary>
public class ConnectionPageViewModelTests
{
    private readonly IConnectionService _connectionService;
    private readonly IDeviceStore _deviceStore;
    private readonly Device _device;

    public ConnectionPageViewModelTests()
    {
        _connectionService = Substitute.For<IConnectionService>();
        _deviceStore = Substitute.For<IDeviceStore>();
        _device = new Device { Name = "Test", Hostname = "test.local" };
        _deviceStore.GetById(_device.Id).Returns(_device);
    }

    public Remort.DeviceWindow.ConnectionPageViewModel CreateSut()
    {
        return new Remort.DeviceWindow.ConnectionPageViewModel(_device, _connectionService, _deviceStore);
    }

    [Fact]
    public void Constructor_SetsServerOnConnectionService()
    {
        CreateSut();

        _connectionService.Server.Should().Be("test.local");
    }

    [Fact]
    public void Constructor_InitializesDisconnectedState()
    {
        Remort.DeviceWindow.ConnectionPageViewModel sut = CreateSut();

        sut.ConnectionState.Should().Be(ConnectionState.Disconnected);
        sut.StatusText.Should().Be("Disconnected");
        sut.ButtonLabel.Should().Be("Connect");
    }

    [Fact]
    public void ToggleConnection_WhenDisconnected_CallsConnect()
    {
        Remort.DeviceWindow.ConnectionPageViewModel sut = CreateSut();

        sut.ToggleConnectionCommand.Execute(null);

        _connectionService.Received(1).Connect();
        sut.ConnectionState.Should().Be(ConnectionState.Connecting);
    }

    [Fact]
    public void ToggleConnection_WhenConnected_CallsDisconnect()
    {
        Remort.DeviceWindow.ConnectionPageViewModel sut = CreateSut();

        // Simulate connected state
        _connectionService.Connected += Raise.Event();

        sut.ToggleConnectionCommand.Execute(null);

        _connectionService.Received(1).Disconnect();
    }

    [Fact]
    public void OnConnected_UpdatesStateAndStatus()
    {
        Remort.DeviceWindow.ConnectionPageViewModel sut = CreateSut();

        _connectionService.Connected += Raise.Event();

        sut.ConnectionState.Should().Be(ConnectionState.Connected);
        sut.StatusText.Should().Contain("Connected");
        sut.ButtonLabel.Should().Be("Disconnect");
    }

    [Fact]
    public void OnDisconnected_UpdatesStateAndStatus()
    {
        Remort.DeviceWindow.ConnectionPageViewModel sut = CreateSut();
        _connectionService.Connected += Raise.Event();

        _connectionService.Disconnected += Raise.EventWith(new DisconnectedEventArgs(0));

        sut.ConnectionState.Should().Be(ConnectionState.Disconnected);
        sut.StatusText.Should().Be("Disconnected");
        sut.ButtonLabel.Should().Be("Connect");
    }

    [Fact]
    public void OnRetriesExhausted_ShowsErrorInSubstatus()
    {
        Remort.DeviceWindow.ConnectionPageViewModel sut = CreateSut();
        sut.ToggleConnectionCommand.Execute(null);

        _connectionService.RetriesExhausted += Raise.EventWith(
            new RetriesExhaustedEventArgs(3, "Timeout"));

        sut.ConnectionState.Should().Be(ConnectionState.Disconnected);
        sut.SubstatusText.Should().Contain("Timeout");
        sut.ButtonLabel.Should().Be("Connect");
    }

    [Fact]
    public void AutoconnectOnStart_WhenChanged_PersistsToStore()
    {
        Remort.DeviceWindow.ConnectionPageViewModel sut = CreateSut();

        sut.AutoconnectOnStart = true;

        _deviceStore.Received(1).Update(Arg.Is<Device>(d => d.ConnectionSettings.AutoconnectOnStart));
        _deviceStore.Received(1).Save();
    }

    [Fact]
    public void AutoconnectWhenVisible_WhenChanged_PersistsToStore()
    {
        Remort.DeviceWindow.ConnectionPageViewModel sut = CreateSut();

        sut.AutoconnectWhenVisible = true;

        _deviceStore.Received(1).Update(Arg.Is<Device>(d => d.ConnectionSettings.AutoconnectWhenVisible));
        _deviceStore.Received(1).Save();
    }

    [Fact]
    public void StartOnStartup_WhenChanged_PersistsToStore()
    {
        Remort.DeviceWindow.ConnectionPageViewModel sut = CreateSut();

        sut.StartOnStartup = true;

        _deviceStore.Received(1).Update(Arg.Is<Device>(d => d.ConnectionSettings.StartOnStartup));
        _deviceStore.Received(1).Save();
    }

    [Fact]
    public void InitializesCheckboxesFromDeviceSettings()
    {
        var device = new Device
        {
            Name = "Test",
            Hostname = "test.local",
            ConnectionSettings = new DeviceConnectionSettings
            {
                AutoconnectOnStart = true,
                AutoconnectWhenVisible = true,
                StartOnStartup = true,
            },
        };
        _deviceStore.GetById(device.Id).Returns(device);

        var sut = new Remort.DeviceWindow.ConnectionPageViewModel(device, _connectionService, _deviceStore);

        sut.AutoconnectOnStart.Should().BeTrue();
        sut.AutoconnectWhenVisible.Should().BeTrue();
        sut.StartOnStartup.Should().BeTrue();
    }
}
