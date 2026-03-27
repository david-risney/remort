namespace Remort.DevBox;

/// <summary>
/// Represents the runtime state of a Dev Box.
/// </summary>
public enum DevBoxState
{
    /// <summary>The Dev Box state could not be determined.</summary>
    Unknown,

    /// <summary>The Dev Box is running and can accept connections.</summary>
    Running,

    /// <summary>The Dev Box is stopped.</summary>
    Stopped,

    /// <summary>The Dev Box is deallocated.</summary>
    Deallocated,
}
