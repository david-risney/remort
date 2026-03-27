namespace Remort.DevBox;

/// <summary>
/// Provides authentication tokens for accessing the Dev Box service.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Gets a value indicating whether the user is currently signed in.
    /// </summary>
    public bool IsSignedIn { get; }

    /// <summary>
    /// Acquires an access token, prompting the user to sign in if necessary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A bearer access token string.</returns>
    public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
}
