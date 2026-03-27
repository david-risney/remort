using FluentAssertions;
using NSubstitute;
using Remort.Connection;
using Remort.Interop;

namespace Remort.Tests.Connection;

/// <summary>
/// Unit tests for <see cref="RetryingConnectionService"/>.
/// </summary>
public class RetryingConnectionServiceTests
{
    private readonly IRdpClient _rdpClient;
    private readonly RetryingConnectionService _sut;

    public RetryingConnectionServiceTests()
    {
        _rdpClient = Substitute.For<IRdpClient>();
        _sut = new RetryingConnectionService(_rdpClient);
    }

    // --- Connect/Disconnect pass-through ---
    [Fact]
    public void Connect_SetsAttemptAndCallsRdpClient()
    {
        _sut.Server = "host";

        _sut.Connect();

        _rdpClient.Received(1).Connect();
    }

    [Fact]
    public void Disconnect_CallsRdpClientDisconnect()
    {
        _sut.Connect();

        _sut.Disconnect();

        _rdpClient.Received(1).Disconnect();
    }

    [Fact]
    public void Server_GetSet_DelegatesToRdpClient()
    {
        _sut.Server = "myhost";
        _rdpClient.Received(1).Server = "myhost";

        _rdpClient.Server.Returns("myhost");
        _sut.Server.Should().Be("myhost");
    }

    // --- AttemptStarted event ---
    [Fact]
    public void Connect_RaisesAttemptStartedWithAttempt1()
    {
        AttemptStartedEventArgs? received = null;
        _sut.AttemptStarted += (_, e) => received = e;

        _sut.Connect();

        received.Should().NotBeNull();
        received!.Attempt.Should().Be(1);
        received.MaxAttempts.Should().Be(3);
    }

    // --- Successful connection ---
    [Fact]
    public void RdpClientConnected_RaisesConnectedEvent()
    {
        bool raised = false;
        _sut.Connected += (_, _) => raised = true;
        _sut.Connect();

        _rdpClient.Connected += Raise.Event();

        raised.Should().BeTrue();
    }

    // --- Retry on failure ---
    [Fact]
    public void RdpClientDisconnected_WhenAttemptsRemain_RetriesConnect()
    {
        _sut.Connect(); // attempt 1

        _rdpClient.Disconnected += Raise.EventWith(new DisconnectedEventArgs(3));

        // Should have called Connect() twice: initial + 1 retry
        _rdpClient.Received(2).Connect();
    }

    [Fact]
    public void Retry_RaisesAttemptStartedWithIncrementedAttempt()
    {
        var attempts = new List<int>();
        _sut.AttemptStarted += (_, e) => attempts.Add(e.Attempt);

        _sut.Connect(); // attempt 1
        _rdpClient.Disconnected += Raise.EventWith(new DisconnectedEventArgs(3)); // attempt 2
        _rdpClient.Disconnected += Raise.EventWith(new DisconnectedEventArgs(3)); // attempt 3

        attempts.Should().Equal(1, 2, 3);
    }

    // --- Retries exhausted ---
    [Fact]
    public void RetriesExhausted_RaisedWhenMaxAttemptsReached()
    {
        _rdpClient.ExtendedDisconnectReason.Returns(0);
        _rdpClient.GetErrorDescription(3, 0).Returns("Timed out");

        RetriesExhaustedEventArgs? received = null;
        _sut.RetriesExhausted += (_, e) => received = e;

        _sut.Connect(); // attempt 1
        _rdpClient.Disconnected += Raise.EventWith(new DisconnectedEventArgs(3)); // attempt 2
        _rdpClient.Disconnected += Raise.EventWith(new DisconnectedEventArgs(3)); // attempt 3
        _rdpClient.Disconnected += Raise.EventWith(new DisconnectedEventArgs(3)); // exhausted

        received.Should().NotBeNull();
        received!.TotalAttempts.Should().Be(3);
        received.LastErrorDescription.Should().Be("Timed out");
    }

    [Fact]
    public void RetriesExhausted_DoesNotRetryFurther()
    {
        _rdpClient.ExtendedDisconnectReason.Returns(0);
        _rdpClient.GetErrorDescription(Arg.Any<int>(), Arg.Any<int>()).Returns(string.Empty);

        _sut.Connect(); // attempt 1
        _rdpClient.Disconnected += Raise.EventWith(new DisconnectedEventArgs(3)); // attempt 2
        _rdpClient.Disconnected += Raise.EventWith(new DisconnectedEventArgs(3)); // attempt 3
        _rdpClient.Disconnected += Raise.EventWith(new DisconnectedEventArgs(3)); // exhausted

        // Initial + 2 retries = 3 total Connect() calls
        _rdpClient.Received(3).Connect();
    }

    // --- User cancellation ---
    [Fact]
    public void Disconnect_DuringRetry_StopsRetrying()
    {
        bool disconnectedRaised = false;
        _sut.Disconnected += (_, _) => disconnectedRaised = true;

        _sut.Connect(); // attempt 1
        _rdpClient.Disconnected += Raise.EventWith(new DisconnectedEventArgs(3)); // triggers retry → attempt 2

        _sut.Disconnect(); // user cancels
        _rdpClient.Disconnected += Raise.EventWith(new DisconnectedEventArgs(1)); // post-disconnect event

        disconnectedRaised.Should().BeTrue();

        // Should NOT have retried after user disconnect: initial + 1 retry + no more
        _rdpClient.Received(2).Connect();
    }

    [Fact]
    public void Disconnect_DuringRetry_DoesNotRaiseRetriesExhausted()
    {
        bool exhaustedRaised = false;
        _sut.RetriesExhausted += (_, _) => exhaustedRaised = true;

        _sut.Connect();
        _sut.Disconnect();
        _rdpClient.Disconnected += Raise.EventWith(new DisconnectedEventArgs(1));

        exhaustedRaised.Should().BeFalse();
    }

    // --- Post-connected disconnect (not a retry scenario) ---
    [Fact]
    public void RdpClientDisconnected_AfterConnected_DoesNotRetry()
    {
        bool disconnectedRaised = false;
        _sut.Disconnected += (_, _) => disconnectedRaised = true;

        _sut.Connect();
        _rdpClient.Connected += Raise.Event(); // session established
        _rdpClient.Disconnected += Raise.EventWith(new DisconnectedEventArgs(2)); // remote drop

        disconnectedRaised.Should().BeTrue();
        _rdpClient.Received(1).Connect(); // only the initial call
    }

    // --- Custom retry policy ---
    [Fact]
    public void CustomRetryPolicy_LimitsAttempts()
    {
        _rdpClient.ExtendedDisconnectReason.Returns(0);
        _rdpClient.GetErrorDescription(Arg.Any<int>(), Arg.Any<int>()).Returns("Error");

        _sut.RetryPolicy = new ConnectionRetryPolicy(MaxAttempts: 5);

        var attempts = new List<int>();
        _sut.AttemptStarted += (_, e) => attempts.Add(e.Attempt);

        _sut.Connect();
        for (int i = 0; i < 5; i++)
        {
            _rdpClient.Disconnected += Raise.EventWith(new DisconnectedEventArgs(3));
        }

        attempts.Should().Equal(1, 2, 3, 4, 5);
        _rdpClient.Received(5).Connect();
    }

    [Fact]
    public void RetryPolicy_ZeroMaxAttempts_MakesOneAttemptWithNoRetries()
    {
        _rdpClient.ExtendedDisconnectReason.Returns(0);
        _rdpClient.GetErrorDescription(Arg.Any<int>(), Arg.Any<int>()).Returns("Error");

        _sut.RetryPolicy = new ConnectionRetryPolicy(MaxAttempts: 0);

        RetriesExhaustedEventArgs? exhausted = null;
        _sut.RetriesExhausted += (_, e) => exhausted = e;

        _sut.Connect();
        _rdpClient.Disconnected += Raise.EventWith(new DisconnectedEventArgs(3));

        exhausted.Should().NotBeNull();
        exhausted!.TotalAttempts.Should().Be(1);
        _rdpClient.Received(1).Connect();
    }

    [Fact]
    public void RetryPolicy_OneMaxAttempt_MakesOneAttemptWithNoRetries()
    {
        _rdpClient.ExtendedDisconnectReason.Returns(0);
        _rdpClient.GetErrorDescription(Arg.Any<int>(), Arg.Any<int>()).Returns("Error");

        _sut.RetryPolicy = new ConnectionRetryPolicy(MaxAttempts: 1);

        RetriesExhaustedEventArgs? exhausted = null;
        _sut.RetriesExhausted += (_, e) => exhausted = e;

        _sut.Connect();
        _rdpClient.Disconnected += Raise.EventWith(new DisconnectedEventArgs(3));

        exhausted.Should().NotBeNull();
        exhausted!.TotalAttempts.Should().Be(1);
        _rdpClient.Received(1).Connect();
    }

    // --- Mid-retry success ---
    [Fact]
    public void RdpClientConnected_DuringRetry_RaisesConnectedAndStopsRetrying()
    {
        bool connected = false;
        _sut.Connected += (_, _) => connected = true;

        _sut.Connect(); // attempt 1
        _rdpClient.Disconnected += Raise.EventWith(new DisconnectedEventArgs(3)); // retry → attempt 2
        _rdpClient.Connected += Raise.Event(); // success on attempt 2

        connected.Should().BeTrue();
        _rdpClient.Received(2).Connect();
    }
}
