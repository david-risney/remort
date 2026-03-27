using FluentAssertions;
using NSubstitute;
using Remort.Connection;
using Remort.Settings;
using Remort.VirtualDesktop;

namespace Remort.Tests.VirtualDesktop;

/// <summary>
/// Unit tests for desktop switching behavior in <see cref="MainWindowViewModel"/>.
/// Uses a mocked <see cref="IDesktopSwitcherService"/> to verify ViewModel logic
/// without real virtual desktops, registry, or P/Invoke.
/// </summary>
public class DesktopSwitchViewModelTests
{
    private readonly IConnectionService _connectionService;
    private readonly ISettingsStore _settingsStore;
    private readonly IVirtualDesktopService _virtualDesktopService;
    private readonly IDesktopSwitcherService _desktopSwitcherService;

    public DesktopSwitchViewModelTests()
    {
        _connectionService = Substitute.For<IConnectionService>();
        _settingsStore = Substitute.For<ISettingsStore>();
        _settingsStore.Load().Returns(new AppSettings());
        _virtualDesktopService = Substitute.For<IVirtualDesktopService>();
        _virtualDesktopService.IsSupported.Returns(true);
        _desktopSwitcherService = Substitute.For<IDesktopSwitcherService>();
    }

    [Fact]
    public void IsDesktopSwitcherSupported_ReflectsServiceIsSupported_WhenTrue()
    {
        _desktopSwitcherService.IsSupported.Returns(true);

        MainWindowViewModel sut = CreateViewModel();

        sut.IsDesktopSwitcherSupported.Should().BeTrue();
    }

    [Fact]
    public void IsDesktopSwitcherSupported_ReflectsServiceIsSupported_WhenFalse()
    {
        _desktopSwitcherService.IsSupported.Returns(false);

        MainWindowViewModel sut = CreateViewModel();

        sut.IsDesktopSwitcherSupported.Should().BeFalse();
    }

    [Fact]
    public void IsDesktopSwitcherSupported_IsFalse_WhenNoServiceProvided()
    {
        var sut = new MainWindowViewModel(_connectionService, settingsStore: _settingsStore);

        sut.IsDesktopSwitcherSupported.Should().BeFalse();
    }

    [Fact]
    public void DesktopList_IsPopulated_OnInitialization()
    {
        var desktops = CreateTestDesktops(3);
        _desktopSwitcherService.IsSupported.Returns(true);
        _desktopSwitcherService.GetDesktops().Returns(desktops);
        _desktopSwitcherService.GetCurrentDesktopIndex().Returns(0);

        MainWindowViewModel sut = CreateViewModel();

        sut.DesktopList.Should().HaveCount(3);
        sut.DesktopList[0].Name.Should().Be("Desktop 1");
        sut.DesktopList[1].Name.Should().Be("Desktop 2");
        sut.DesktopList[2].Name.Should().Be("Desktop 3");
    }

    [Fact]
    public void DesktopList_IsEmpty_WhenNotSupported()
    {
        _desktopSwitcherService.IsSupported.Returns(false);

        MainWindowViewModel sut = CreateViewModel();

        sut.DesktopList.Should().BeEmpty();
    }

    [Fact]
    public void CurrentDesktop_IsSet_OnInitialization()
    {
        var desktops = CreateTestDesktops(3);
        _desktopSwitcherService.IsSupported.Returns(true);
        _desktopSwitcherService.GetDesktops().Returns(desktops);
        _desktopSwitcherService.GetCurrentDesktopIndex().Returns(1);

        MainWindowViewModel sut = CreateViewModel();

        sut.CurrentDesktop.Should().NotBeNull();
        sut.CurrentDesktop!.Name.Should().Be("Desktop 2");
    }

    [Fact]
    public void SwitchToDesktopCommand_CallsService_WithCorrectIndices()
    {
        var desktops = CreateTestDesktops(3);
        _desktopSwitcherService.IsSupported.Returns(true);
        _desktopSwitcherService.GetDesktops().Returns(desktops);
        _desktopSwitcherService.GetCurrentDesktopIndex().Returns(0);

        MainWindowViewModel sut = CreateViewModel();

        sut.SwitchToDesktopCommand.Execute(desktops[2]);

        _desktopSwitcherService.Received(1).SwitchToDesktop(2, 0);
    }

    [Fact]
    public void SwitchToDesktopCommand_IsNoOp_WhenTargetIsCurrentDesktop()
    {
        var desktops = CreateTestDesktops(3);
        _desktopSwitcherService.IsSupported.Returns(true);
        _desktopSwitcherService.GetDesktops().Returns(desktops);
        _desktopSwitcherService.GetCurrentDesktopIndex().Returns(1);

        MainWindowViewModel sut = CreateViewModel();

        sut.SwitchToDesktopCommand.Execute(desktops[1]);

        _desktopSwitcherService.DidNotReceive().SwitchToDesktop(Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public void SwitchToDesktopCommand_IsNoOp_WhenTargetIsNull()
    {
        var desktops = CreateTestDesktops(2);
        _desktopSwitcherService.IsSupported.Returns(true);
        _desktopSwitcherService.GetDesktops().Returns(desktops);
        _desktopSwitcherService.GetCurrentDesktopIndex().Returns(0);

        MainWindowViewModel sut = CreateViewModel();

        sut.SwitchToDesktopCommand.Execute(null);

        _desktopSwitcherService.DidNotReceive().SwitchToDesktop(Arg.Any<int>(), Arg.Any<int>());
    }

    // --- User Story 2: Active desktop tracking via DesktopsChanged event ---
    [Fact]
    public void DesktopsChanged_UpdatesCurrentDesktop()
    {
        var desktops = CreateTestDesktops(3);
        _desktopSwitcherService.IsSupported.Returns(true);
        _desktopSwitcherService.GetDesktops().Returns(desktops);
        _desktopSwitcherService.GetCurrentDesktopIndex().Returns(0);

        MainWindowViewModel sut = CreateViewModel();
        sut.CurrentDesktop!.Index.Should().Be(0);

        // Simulate external switch to desktop 2.
        _desktopSwitcherService.GetCurrentDesktopIndex().Returns(2);
        _desktopSwitcherService.DesktopsChanged += Raise.Event();

        sut.CurrentDesktop!.Index.Should().Be(2);
    }

    [Fact]
    public void DesktopsChanged_AddsNewDesktops()
    {
        var desktops = CreateTestDesktops(2);
        _desktopSwitcherService.IsSupported.Returns(true);
        _desktopSwitcherService.GetDesktops().Returns(desktops);
        _desktopSwitcherService.GetCurrentDesktopIndex().Returns(0);

        MainWindowViewModel sut = CreateViewModel();
        sut.DesktopList.Should().HaveCount(2);

        // Simulate a desktop being added.
        var updatedDesktops = CreateTestDesktops(3);
        _desktopSwitcherService.GetDesktops().Returns(updatedDesktops);
        _desktopSwitcherService.GetCurrentDesktopIndex().Returns(0);
        _desktopSwitcherService.DesktopsChanged += Raise.Event();

        sut.DesktopList.Should().HaveCount(3);
    }

    [Fact]
    public void DesktopsChanged_RemovesDeletedDesktops()
    {
        var desktops = CreateTestDesktops(3);
        _desktopSwitcherService.IsSupported.Returns(true);
        _desktopSwitcherService.GetDesktops().Returns(desktops);
        _desktopSwitcherService.GetCurrentDesktopIndex().Returns(0);

        MainWindowViewModel sut = CreateViewModel();
        sut.DesktopList.Should().HaveCount(3);

        // Simulate a desktop being removed.
        var updatedDesktops = CreateTestDesktops(2);
        _desktopSwitcherService.GetDesktops().Returns(updatedDesktops);
        _desktopSwitcherService.GetCurrentDesktopIndex().Returns(0);
        _desktopSwitcherService.DesktopsChanged += Raise.Event();

        sut.DesktopList.Should().HaveCount(2);
    }

    [Fact]
    public void DesktopsChanged_DoesNotTriggerSwitchToDesktop()
    {
        var desktops = CreateTestDesktops(3);
        _desktopSwitcherService.IsSupported.Returns(true);
        _desktopSwitcherService.GetDesktops().Returns(desktops);
        _desktopSwitcherService.GetCurrentDesktopIndex().Returns(0);

        MainWindowViewModel sut = CreateViewModel();

        // Simulate external switch to desktop 1.
        _desktopSwitcherService.GetCurrentDesktopIndex().Returns(1);
        _desktopSwitcherService.DesktopsChanged += Raise.Event();

        // The service should NOT have been called to switch (it was an external switch).
        _desktopSwitcherService.DidNotReceive().SwitchToDesktop(Arg.Any<int>(), Arg.Any<int>());
    }

    // --- User Story 3: Keyboard shortcut commands ---
    [Fact]
    public void SwitchToNextDesktopCommand_SwitchesToNextDesktop()
    {
        var desktops = CreateTestDesktops(3);
        _desktopSwitcherService.IsSupported.Returns(true);
        _desktopSwitcherService.GetDesktops().Returns(desktops);
        _desktopSwitcherService.GetCurrentDesktopIndex().Returns(1);

        MainWindowViewModel sut = CreateViewModel();

        sut.SwitchToNextDesktopCommand.Execute(null);

        _desktopSwitcherService.Received(1).SwitchToDesktop(2, 1);
    }

    [Fact]
    public void SwitchToPreviousDesktopCommand_SwitchesToPreviousDesktop()
    {
        var desktops = CreateTestDesktops(3);
        _desktopSwitcherService.IsSupported.Returns(true);
        _desktopSwitcherService.GetDesktops().Returns(desktops);
        _desktopSwitcherService.GetCurrentDesktopIndex().Returns(1);

        MainWindowViewModel sut = CreateViewModel();

        sut.SwitchToPreviousDesktopCommand.Execute(null);

        _desktopSwitcherService.Received(1).SwitchToDesktop(0, 1);
    }

    [Fact]
    public void SwitchToNextDesktopCommand_IsNoOp_OnLastDesktop()
    {
        var desktops = CreateTestDesktops(3);
        _desktopSwitcherService.IsSupported.Returns(true);
        _desktopSwitcherService.GetDesktops().Returns(desktops);
        _desktopSwitcherService.GetCurrentDesktopIndex().Returns(2);

        MainWindowViewModel sut = CreateViewModel();

        sut.SwitchToNextDesktopCommand.Execute(null);

        _desktopSwitcherService.DidNotReceive().SwitchToDesktop(Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public void SwitchToPreviousDesktopCommand_IsNoOp_OnFirstDesktop()
    {
        var desktops = CreateTestDesktops(3);
        _desktopSwitcherService.IsSupported.Returns(true);
        _desktopSwitcherService.GetDesktops().Returns(desktops);
        _desktopSwitcherService.GetCurrentDesktopIndex().Returns(0);

        MainWindowViewModel sut = CreateViewModel();

        sut.SwitchToPreviousDesktopCommand.Execute(null);

        _desktopSwitcherService.DidNotReceive().SwitchToDesktop(Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public void SwitchToNextDesktopCommand_IsNoOp_WhenNotSupported()
    {
        _desktopSwitcherService.IsSupported.Returns(false);

        MainWindowViewModel sut = CreateViewModel();

        sut.SwitchToNextDesktopCommand.Execute(null);

        _desktopSwitcherService.DidNotReceive().SwitchToDesktop(Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public void SwitchToPreviousDesktopCommand_IsNoOp_WhenOnlyOneDesktop()
    {
        var desktops = CreateTestDesktops(1);
        _desktopSwitcherService.IsSupported.Returns(true);
        _desktopSwitcherService.GetDesktops().Returns(desktops);
        _desktopSwitcherService.GetCurrentDesktopIndex().Returns(0);

        MainWindowViewModel sut = CreateViewModel();

        sut.SwitchToPreviousDesktopCommand.Execute(null);

        _desktopSwitcherService.DidNotReceive().SwitchToDesktop(Arg.Any<int>(), Arg.Any<int>());
    }

    private static List<VirtualDesktopInfo> CreateTestDesktops(int count)
    {
        var list = new List<VirtualDesktopInfo>();
        for (int i = 0; i < count; i++)
        {
            list.Add(new VirtualDesktopInfo(Guid.NewGuid(), $"Desktop {i + 1}", i));
        }

        return list;
    }

    private MainWindowViewModel CreateViewModel()
    {
        return new MainWindowViewModel(
            _connectionService,
            settingsStore: _settingsStore,
            virtualDesktopService: _virtualDesktopService,
            desktopSwitcherService: _desktopSwitcherService);
    }
}
