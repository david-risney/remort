namespace Remort.DevBox;

/// <summary>
/// Represents a parsed hostname that may or may not refer to a Dev Box.
/// </summary>
/// <param name="ShortName">The short (unqualified) name portion.</param>
/// <param name="FullyQualifiedName">The full input string.</param>
/// <param name="IsDevBox">Whether the input is recognized as a Dev Box identifier.</param>
public sealed record DevBoxIdentifier(string ShortName, string FullyQualifiedName, bool IsDevBox)
{
    private const string DevBoxSuffix = ".devbox.microsoft.com";

    /// <summary>
    /// Parses user input and determines whether it refers to a Dev Box.
    /// </summary>
    /// <param name="input">The hostname or Dev Box name entered by the user.</param>
    /// <returns>A <see cref="DevBoxIdentifier"/> describing the parsed input.</returns>
    public static DevBoxIdentifier Parse(string input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        string trimmed = input.Trim();

        // IP addresses (v4 or v6-style) are never Dev Box identifiers.
        if (System.Net.IPAddress.TryParse(trimmed, out _))
        {
            return new DevBoxIdentifier(trimmed, trimmed, IsDevBox: false);
        }

        // Fully qualified Dev Box hostnames end with .devbox.microsoft.com.
        if (trimmed.EndsWith(DevBoxSuffix, StringComparison.OrdinalIgnoreCase))
        {
            string shortName = trimmed[..trimmed.IndexOf('.', StringComparison.Ordinal)];
            return new DevBoxIdentifier(shortName, trimmed, IsDevBox: true);
        }

        // Everything else (single-label or multi-label hostnames) is a standard hostname.
        // Users must use the .devbox.microsoft.com suffix to trigger Dev Box resolution.
        return new DevBoxIdentifier(trimmed, trimmed, IsDevBox: false);
    }
}
