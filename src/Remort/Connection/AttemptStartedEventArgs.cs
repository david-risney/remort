namespace Remort.Connection;

/// <summary>
/// Event data for the start of a connection attempt.
/// </summary>
/// <param name="attempt">The 1-based attempt number.</param>
/// <param name="maxAttempts">The maximum number of attempts allowed.</param>
public sealed class AttemptStartedEventArgs(int attempt, int maxAttempts) : EventArgs
{
    /// <summary>Gets the 1-based attempt number.</summary>
    public int Attempt { get; } = attempt;

    /// <summary>Gets the maximum number of attempts allowed.</summary>
    public int MaxAttempts { get; } = maxAttempts;
}
