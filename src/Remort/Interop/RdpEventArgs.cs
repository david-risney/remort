namespace Remort.Interop;

#pragma warning disable SA1402 // File may only contain a single type — grouped trivial EventArgs per research.md R5
#pragma warning disable SA1649 // File name should match first type name — intentionally named RdpEventArgs.cs for grouping

/// <summary>
/// Event data for an RDP disconnection, carrying the disconnect reason code.
/// </summary>
public sealed class DisconnectedEventArgs(int reason) : EventArgs
{
    /// <summary>Gets the RDP disconnect reason code.</summary>
    public int Reason { get; } = reason;
}

/// <summary>
/// Event data for an RDP warning event.
/// </summary>
public sealed class WarningEventArgs(int warningCode) : EventArgs
{
    /// <summary>Gets the warning code.</summary>
    public int WarningCode { get; } = warningCode;
}

/// <summary>
/// Event data for an RDP fatal error event.
/// </summary>
public sealed class FatalErrorEventArgs(int errorCode) : EventArgs
{
    /// <summary>Gets the error code.</summary>
    public int ErrorCode { get; } = errorCode;
}
