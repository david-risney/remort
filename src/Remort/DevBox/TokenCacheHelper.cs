using System.IO;
using Microsoft.Identity.Client;

namespace Remort.DevBox;

/// <summary>
/// Provides file-based token cache serialization for MSAL so users
/// are not prompted to sign in on every application launch.
/// </summary>
internal static class TokenCacheHelper
{
    private static readonly string s_cacheFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Remort",
        "msal_token_cache.bin");

    private static readonly object s_fileLock = new();

    /// <summary>
    /// Enables token cache serialization on the given <paramref name="tokenCache"/>.
    /// </summary>
    /// <param name="tokenCache">The MSAL token cache to persist.</param>
    public static void EnableSerialization(ITokenCache tokenCache)
    {
        tokenCache.SetBeforeAccess(BeforeAccessNotification);
        tokenCache.SetAfterAccess(AfterAccessNotification);
    }

    private static void BeforeAccessNotification(TokenCacheNotificationArgs args)
    {
        lock (s_fileLock)
        {
            if (File.Exists(s_cacheFilePath))
            {
                args.TokenCache.DeserializeMsalV3(File.ReadAllBytes(s_cacheFilePath));
            }
        }
    }

    private static void AfterAccessNotification(TokenCacheNotificationArgs args)
    {
        if (!args.HasStateChanged)
        {
            return;
        }

        lock (s_fileLock)
        {
            string? directory = Path.GetDirectoryName(s_cacheFilePath);
            if (directory is not null)
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(s_cacheFilePath, args.TokenCache.SerializeMsalV3());
        }
    }
}
