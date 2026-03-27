namespace Remort.Connection;

/// <summary>
/// Event data when all retry attempts have been exhausted.
/// </summary>
/// <param name="totalAttempts">The total number of attempts made.</param>
/// <param name="lastErrorDescription">The error description from the last failed attempt.</param>
public sealed class RetriesExhaustedEventArgs(int totalAttempts, string lastErrorDescription) : EventArgs
{
    /// <summary>Gets the total number of attempts made.</summary>
    public int TotalAttempts { get; } = totalAttempts;

    /// <summary>Gets the error description from the last failed attempt.</summary>
    public string LastErrorDescription { get; } = lastErrorDescription;
}
