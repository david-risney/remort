namespace Remort.Connection;

/// <summary>
/// Represents the lifecycle state of an RDP connection.
/// </summary>
public enum ConnectionState
{
    /// <summary>No active session.</summary>
    Disconnected,

    /// <summary>Dev Box name resolution in progress.</summary>
    Resolving,

    /// <summary>Connection attempt in progress.</summary>
    Connecting,

    /// <summary>Active RDP session.</summary>
    Connected,
}
