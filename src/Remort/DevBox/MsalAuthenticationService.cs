using Microsoft.Identity.Client;

namespace Remort.DevBox;

/// <summary>
/// Authenticates the user via MSAL for Dev Box service access.
/// Uses interactive browser sign-in on first call, silent acquisition subsequently.
/// </summary>
public sealed class MsalAuthenticationService : IAuthenticationService
{
    // Azure Developer CLI client ID — widely used for Dev Box / Dev Center scenarios.
    private const string ClientId = "04b07795-ee78-4fb6-aaf7-40842b66e2a0";
    private static readonly string[] s_scopes = ["https://devcenter.azure.com/.default"];

    private readonly IPublicClientApplication _pca;

    /// <summary>
    /// Initializes a new instance of the <see cref="MsalAuthenticationService"/> class.
    /// </summary>
    public MsalAuthenticationService()
    {
        _pca = PublicClientApplicationBuilder
            .Create(ClientId)
            .WithAuthority(AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount)
            .WithDefaultRedirectUri()
            .Build();

        // Enable token cache serialization for persistence across sessions.
        TokenCacheHelper.EnableSerialization(_pca.UserTokenCache);
    }

    /// <inheritdoc/>
    public bool IsSignedIn
    {
        get
        {
            IEnumerable<IAccount> accounts = _pca.GetAccountsAsync()
                .ConfigureAwait(false).GetAwaiter().GetResult();
            return accounts.Any();
        }
    }

    /// <inheritdoc/>
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        IEnumerable<IAccount> accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);
        IAccount? account = accounts.FirstOrDefault();

        AuthenticationResult result;

        if (account is not null)
        {
            try
            {
                result = await _pca.AcquireTokenSilent(s_scopes, account)
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
                return result.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                // Fall through to interactive sign-in.
            }
        }

        result = await _pca.AcquireTokenInteractive(s_scopes)
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        return result.AccessToken;
    }
}
