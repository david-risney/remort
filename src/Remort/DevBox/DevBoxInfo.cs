namespace Remort.DevBox;

/// <summary>
/// Metadata about a resolved Dev Box, including its endpoint and state.
/// </summary>
/// <param name="Name">The Dev Box display name.</param>
/// <param name="ProjectName">The Dev Center project the Dev Box belongs to.</param>
/// <param name="State">The current runtime state.</param>
/// <param name="Endpoint">The resolved RDP endpoint, if available.</param>
public sealed record DevBoxInfo(string Name, string ProjectName, DevBoxState State, DevBoxEndpoint? Endpoint);
