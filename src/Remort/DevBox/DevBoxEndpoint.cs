namespace Remort.DevBox;

/// <summary>
/// Represents a resolved RDP connection endpoint for a Dev Box.
/// </summary>
/// <param name="Host">The RDP host address.</param>
/// <param name="Port">The RDP port number.</param>
public sealed record DevBoxEndpoint(string Host, int Port);
