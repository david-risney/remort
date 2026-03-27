namespace Remort.Connection;

/// <summary>
/// Defines the retry policy for connection attempts.
/// </summary>
/// <param name="MaxAttempts">
/// Maximum number of connection attempts (including the initial attempt).
/// A value of 1 means no retries. A value of 0 means one attempt with no retries
/// (equivalent to 1 for legacy compatibility — see spec edge case).
/// </param>
public readonly record struct ConnectionRetryPolicy(int MaxAttempts = 3)
{
    /// <summary>The default retry policy: 3 attempts.</summary>
    public static readonly ConnectionRetryPolicy Default = new(MaxAttempts: 3);
}
