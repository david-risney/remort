using System.Net.Http;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Remort.Connection;
using Remort.DevBox;
using Remort.Interop;

namespace Remort.Tests.Connection;

/// <summary>
/// Unit tests for <see cref="MainWindowViewModel"/>.
/// </summary>
public class MainWindowViewModelTests
{
    private readonly IConnectionService _connectionService;
    private readonly IDevBoxResolver _devBoxResolver;
    private readonly MainWindowViewModel _sut;

    public MainWindowViewModelTests()
    {
        _connectionService = Substitute.For<IConnectionService>();
        _devBoxResolver = Substitute.For<IDevBoxResolver>();
        _sut = new MainWindowViewModel(_connectionService, _devBoxResolver);
    }

    // --- US1: Connect scenario tests ---
    [Fact]
    public void ConnectCommand_WithValidHostname_TransitionsToConnecting()
    {
        _sut.Hostname = "myserver.contoso.com";

        _sut.ConnectCommand.Execute(null);

        _sut.ConnectionState.Should().Be(ConnectionState.Connecting);
    }

    [Fact]
    public void ConnectCommand_WithValidHostname_TrimsHostnameBeforeAssigning()
    {
        _sut.Hostname = "  myserver.contoso.com  ";

        _sut.ConnectCommand.Execute(null);

        _connectionService.Received(1).Server = "myserver.contoso.com";
    }

    [Fact]
    public void ConnectCommand_WithValidHostname_CallsServiceConnect()
    {
        _sut.Hostname = "myserver.contoso.com";

        _sut.ConnectCommand.Execute(null);

        _connectionService.Received(1).Connect();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ConnectCommand_WithEmptyOrWhitespaceHostname_CannotExecute(string? hostname)
    {
        _sut.Hostname = hostname ?? string.Empty;

        _sut.ConnectCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ConnectCommand_WhenConnecting_CannotExecute()
    {
        _sut.Hostname = "myserver.contoso.com";
        _sut.ConnectCommand.Execute(null); // transitions to Connecting

        _sut.ConnectCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ServiceConnectedEvent_TransitionsToConnected()
    {
        _sut.Hostname = "myserver.contoso.com";
        _sut.ConnectCommand.Execute(null);

        _connectionService.Connected += Raise.Event();

        _sut.ConnectionState.Should().Be(ConnectionState.Connected);
    }

    [Fact]
    public void ServiceDisconnectedEvent_ReturnsToDisconnected()
    {
        _sut.Hostname = "myserver.contoso.com";
        _sut.ConnectCommand.Execute(null);
        _connectionService.Connected += Raise.Event();

        _connectionService.Disconnected += Raise.EventWith(new DisconnectedEventArgs(1));

        _sut.ConnectionState.Should().Be(ConnectionState.Disconnected);
    }

    // --- US2: Disconnect scenario tests ---
    [Fact]
    public void DisconnectCommand_WhenConnected_CallsServiceDisconnect()
    {
        _sut.Hostname = "myserver.contoso.com";
        _sut.ConnectCommand.Execute(null);
        _connectionService.Connected += Raise.Event();

        _sut.DisconnectCommand.Execute(null);

        _connectionService.Received(1).Disconnect();
    }

    [Fact]
    public void DisconnectCommand_WhenDisconnected_CannotExecute()
    {
        _sut.DisconnectCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void DisconnectCommand_WhenConnecting_CanExecute()
    {
        _sut.Hostname = "myserver.contoso.com";
        _sut.ConnectCommand.Execute(null);

        _sut.DisconnectCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void DisconnectCommand_AfterDisconnect_HostnameAndConnectReEnabled()
    {
        _sut.Hostname = "myserver.contoso.com";
        _sut.ConnectCommand.Execute(null);
        _connectionService.Connected += Raise.Event();
        _connectionService.Disconnected += Raise.EventWith(new DisconnectedEventArgs(0));

        _sut.ConnectionState.Should().Be(ConnectionState.Disconnected);
        _sut.ConnectCommand.CanExecute(null).Should().BeTrue();
    }

    // --- US3: StatusText scenario tests ---
    [Fact]
    public void StatusText_Initially_IsDisconnected()
    {
        _sut.StatusText.Should().Be("Disconnected");
    }

    [Fact]
    public void StatusText_OnAttemptStarted_ShowsAttemptProgress()
    {
        _sut.Hostname = "myserver.contoso.com";
        _sut.ConnectCommand.Execute(null);

        _connectionService.AttemptStarted += Raise.EventWith(new AttemptStartedEventArgs(1, 3));

        _sut.StatusText.Should().Be("Connecting\u2026 (attempt 1 of 3)");
    }

    [Fact]
    public void StatusText_WhenConnected_ShowsHostname()
    {
        _connectionService.Server.Returns("myserver.contoso.com");
        _sut.Hostname = "myserver.contoso.com";
        _sut.ConnectCommand.Execute(null);

        _connectionService.Connected += Raise.Event();

        _sut.StatusText.Should().Be("Connected to myserver.contoso.com");
    }

    [Fact]
    public void StatusText_WhenDisconnected_ShowsDisconnected()
    {
        _sut.Hostname = "myserver.contoso.com";
        _sut.ConnectCommand.Execute(null);
        _connectionService.Connected += Raise.Event();

        _connectionService.Disconnected += Raise.EventWith(new DisconnectedEventArgs(1));

        _sut.StatusText.Should().Be("Disconnected");
    }

    [Fact]
    public void StatusText_WhenRetriesExhaustedWithReason_ShowsFailureMessage()
    {
        _sut.Hostname = "myserver.contoso.com";
        _sut.ConnectCommand.Execute(null);

        _connectionService.RetriesExhausted += Raise.EventWith(
            new RetriesExhaustedEventArgs(3, "Connection timed out"));

        _sut.StatusText.Should().Be("Connection failed after 3 attempts: Connection timed out");
    }

    [Fact]
    public void StatusText_WhenRetriesExhaustedWithoutReason_ShowsGenericFailure()
    {
        _sut.Hostname = "myserver.contoso.com";
        _sut.ConnectCommand.Execute(null);

        _connectionService.RetriesExhausted += Raise.EventWith(
            new RetriesExhaustedEventArgs(3, string.Empty));

        _sut.StatusText.Should().Be("Connection failed after 3 attempts");
    }

    [Fact]
    public void RetriesExhausted_ReturnsToDisconnectedState()
    {
        _sut.Hostname = "myserver.contoso.com";
        _sut.ConnectCommand.Execute(null);

        _connectionService.RetriesExhausted += Raise.EventWith(
            new RetriesExhaustedEventArgs(3, "Error"));

        _sut.ConnectionState.Should().Be(ConnectionState.Disconnected);
    }

    // --- US1 Dev Box: Resolution tests ---
    [Fact]
    public async Task ConnectCommand_WithDevBoxName_CallsResolverAndConnects()
    {
        DevBoxEndpoint endpoint = new("resolved-host.example.com", 3389);
        DevBoxInfo info = new("my-devbox", "MyProject", DevBoxState.Running, endpoint);
        _devBoxResolver.ResolveAsync(Arg.Any<DevBoxIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(info));

        _sut.Hostname = "my-devbox.project.devbox.microsoft.com";
        await _sut.ConnectCommand.ExecuteAsync(null);

        await _devBoxResolver.Received(1)
            .ResolveAsync(Arg.Is<DevBoxIdentifier>(id => id.IsDevBox && id.ShortName == "my-devbox"), Arg.Any<CancellationToken>());
        _connectionService.Received(1).Server = "resolved-host.example.com";
        _connectionService.Received(1).Connect();
        _sut.ConnectionState.Should().Be(ConnectionState.Connecting);
    }

    [Fact]
    public async Task ConnectCommand_WithDevBoxName_ShowsResolvingStatusFirst()
    {
        string? capturedStatus = null;
        var tcs = new TaskCompletionSource<DevBoxInfo>();
        _devBoxResolver.ResolveAsync(Arg.Any<DevBoxIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        _sut.Hostname = "my-devbox.project.devbox.microsoft.com";

        // Start command but don't await yet — resolver is blocked
        Task connectTask = _sut.ConnectCommand.ExecuteAsync(null);

        // Capture state while resolving
        capturedStatus = _sut.StatusText;
        _sut.ConnectionState.Should().Be(ConnectionState.Resolving);
        capturedStatus.Should().Contain("Resolving");

        // Complete the resolver
        DevBoxEndpoint endpoint = new("resolved.example.com", 3389);
        DevBoxInfo info = new("my-devbox", "MyProject", DevBoxState.Running, endpoint);
        tcs.SetResult(info);
        await connectTask;

        _sut.ConnectionState.Should().Be(ConnectionState.Connecting);
    }

    [Fact]
    public async Task ConnectCommand_WithStandardHostname_BypassesResolver()
    {
        _sut.Hostname = "myserver.contoso.com";
        await _sut.ConnectCommand.ExecuteAsync(null);

        await _devBoxResolver.DidNotReceive()
            .ResolveAsync(Arg.Any<DevBoxIdentifier>(), Arg.Any<CancellationToken>());
        _connectionService.Received(1).Server = "myserver.contoso.com";
        _connectionService.Received(1).Connect();
    }

    [Fact]
    public async Task ConnectCommand_WithSingleLabelHostname_BypassesResolver()
    {
        _sut.Hostname = "davris-4";
        await _sut.ConnectCommand.ExecuteAsync(null);

        await _devBoxResolver.DidNotReceive()
            .ResolveAsync(Arg.Any<DevBoxIdentifier>(), Arg.Any<CancellationToken>());
        _connectionService.Received(1).Server = "davris-4";
        _connectionService.Received(1).Connect();
    }

    [Fact]
    public async Task ConnectCommand_WithIpAddress_BypassesResolver()
    {
        _sut.Hostname = "10.0.0.1";
        await _sut.ConnectCommand.ExecuteAsync(null);

        await _devBoxResolver.DidNotReceive()
            .ResolveAsync(Arg.Any<DevBoxIdentifier>(), Arg.Any<CancellationToken>());
        _connectionService.Received(1).Server = "10.0.0.1";
        _connectionService.Received(1).Connect();
    }

    // --- US2 Dev Box: Error handling tests ---
    [Fact]
    public async Task ConnectCommand_WhenNotFound_ShowsErrorAndReturnsToDisconnected()
    {
        _devBoxResolver.ResolveAsync(Arg.Any<DevBoxIdentifier>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DevBoxResolutionException(ResolutionFailureReason.NotFound, "Not found"));

        _sut.Hostname = "bad-devbox.project.devbox.microsoft.com";
        await _sut.ConnectCommand.ExecuteAsync(null);

        _sut.ConnectionState.Should().Be(ConnectionState.Disconnected);
        _sut.StatusText.Should().Contain("not found");
    }

    [Fact]
    public async Task ConnectCommand_WhenServiceUnreachable_ShowsErrorAndReturnsToDisconnected()
    {
        _devBoxResolver.ResolveAsync(Arg.Any<DevBoxIdentifier>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DevBoxResolutionException(ResolutionFailureReason.ServiceUnreachable, "Unreachable"));

        _sut.Hostname = "my-devbox.project.devbox.microsoft.com";
        await _sut.ConnectCommand.ExecuteAsync(null);

        _sut.ConnectionState.Should().Be(ConnectionState.Disconnected);
        _sut.StatusText.Should().Contain("Could not reach the Dev Box service");
    }

    [Fact]
    public async Task ConnectCommand_WhenNotRunning_ShowsErrorAndReturnsToDisconnected()
    {
        _devBoxResolver.ResolveAsync(Arg.Any<DevBoxIdentifier>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DevBoxResolutionException(ResolutionFailureReason.NotRunning, "Stopped"));

        _sut.Hostname = "my-devbox.project.devbox.microsoft.com";
        await _sut.ConnectCommand.ExecuteAsync(null);

        _sut.ConnectionState.Should().Be(ConnectionState.Disconnected);
        _sut.StatusText.Should().Contain("not running");
    }

    [Fact]
    public async Task ConnectCommand_WhenUnauthorized_ShowsAuthError()
    {
        _devBoxResolver.ResolveAsync(Arg.Any<DevBoxIdentifier>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DevBoxResolutionException(ResolutionFailureReason.Unauthorized, "Unauthorized"));

        _sut.Hostname = "my-devbox.project.devbox.microsoft.com";
        await _sut.ConnectCommand.ExecuteAsync(null);

        _sut.ConnectionState.Should().Be(ConnectionState.Disconnected);
        _sut.StatusText.Should().Contain("Sign-in is required");
    }

    [Fact]
    public async Task ConnectCommand_WhenUnexpectedError_ShowsGenericMessage()
    {
        _devBoxResolver.ResolveAsync(Arg.Any<DevBoxIdentifier>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network timeout"));

        _sut.Hostname = "my-devbox.project.devbox.microsoft.com";
        await _sut.ConnectCommand.ExecuteAsync(null);

        _sut.ConnectionState.Should().Be(ConnectionState.Disconnected);
        _sut.StatusText.Should().Contain("Could not resolve Dev Box");
    }

    // --- US3 Dev Box: Authentication tests ---
    [Fact]
    public async Task ConnectCommand_WhenSignInCancelled_ShowsSignInRequired()
    {
        _devBoxResolver.ResolveAsync(Arg.Any<DevBoxIdentifier>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException("User cancelled sign-in"));

        _sut.Hostname = "my-devbox.project.devbox.microsoft.com";
        await _sut.ConnectCommand.ExecuteAsync(null);

        _sut.ConnectionState.Should().Be(ConnectionState.Disconnected);
        _sut.StatusText.Should().Contain("Sign-in is required");
    }
}
