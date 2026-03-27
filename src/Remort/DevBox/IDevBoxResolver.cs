namespace Remort.DevBox;

/// <summary>
/// Resolves Dev Box identifiers to RDP connection endpoints.
/// </summary>
public interface IDevBoxResolver
{
    /// <summary>
    /// Resolves a single Dev Box identifier to its connection info.
    /// </summary>
    /// <param name="id">The parsed Dev Box identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved Dev Box information including endpoint.</returns>
    public Task<DevBoxInfo> ResolveAsync(DevBoxIdentifier id, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves all Dev Boxes matching a short name (for disambiguation).
    /// </summary>
    /// <param name="shortName">The short Dev Box name to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of matching Dev Box entries.</returns>
    public Task<IReadOnlyList<DevBoxInfo>> ResolveAllAsync(string shortName, CancellationToken cancellationToken);
}
