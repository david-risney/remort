namespace Remort.DevBox;

/// <summary>
/// The reason a Dev Box resolution failed.
/// </summary>
public enum ResolutionFailureReason
{
    /// <summary>The Dev Box was not found.</summary>
    NotFound,

    /// <summary>The Dev Box is not running.</summary>
    NotRunning,

    /// <summary>The user is not authorized to access this Dev Box.</summary>
    Unauthorized,

    /// <summary>The Dev Box service is unreachable.</summary>
    ServiceUnreachable,
}

/// <summary>
/// Thrown when a Dev Box name cannot be resolved to an RDP endpoint.
/// </summary>
public sealed class DevBoxResolutionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DevBoxResolutionException"/> class.
    /// </summary>
    public DevBoxResolutionException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DevBoxResolutionException"/> class.
    /// </summary>
    /// <param name="message">A human-readable error message.</param>
    public DevBoxResolutionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DevBoxResolutionException"/> class.
    /// </summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public DevBoxResolutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DevBoxResolutionException"/> class.
    /// </summary>
    /// <param name="reason">The reason for the failure.</param>
    /// <param name="message">A human-readable error message.</param>
    public DevBoxResolutionException(ResolutionFailureReason reason, string message)
        : base(message)
    {
        Reason = reason;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DevBoxResolutionException"/> class.
    /// </summary>
    /// <param name="reason">The reason for the failure.</param>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public DevBoxResolutionException(ResolutionFailureReason reason, string message, Exception innerException)
        : base(message, innerException)
    {
        Reason = reason;
    }

    /// <summary>
    /// Gets the reason the resolution failed.
    /// </summary>
    public ResolutionFailureReason Reason { get; }
}
