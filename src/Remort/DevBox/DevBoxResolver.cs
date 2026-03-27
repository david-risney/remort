using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Remort.DevBox;

/// <summary>
/// Resolves Dev Box identifiers to RDP endpoints using the Dev Center REST API.
/// </summary>
public sealed class DevBoxResolver : IDevBoxResolver
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IAuthenticationService _authService;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="DevBoxResolver"/> class.
    /// </summary>
    /// <param name="authService">The authentication service.</param>
    /// <param name="httpClient">HTTP client for Dev Center API calls.</param>
    public DevBoxResolver(IAuthenticationService authService, HttpClient httpClient)
    {
        _authService = authService;
        _httpClient = httpClient;
    }

    /// <inheritdoc/>
    public async Task<DevBoxInfo> ResolveAsync(DevBoxIdentifier id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(id);

        IReadOnlyList<DevBoxInfo> all = await ResolveAllAsync(id.ShortName, cancellationToken).ConfigureAwait(false);

        if (all.Count == 0)
        {
            throw new DevBoxResolutionException(
                ResolutionFailureReason.NotFound,
                $"Dev Box '{id.ShortName}' was not found.");
        }

        // Pick the first match. Disambiguation (multiple matches) is handled by the caller / UI.
        DevBoxInfo info = all[0];

        if (info.State != DevBoxState.Running)
        {
            throw new DevBoxResolutionException(
                ResolutionFailureReason.NotRunning,
                $"Dev Box '{id.ShortName}' is not running (state: {info.State}).");
        }

        if (info.Endpoint is null)
        {
            throw new DevBoxResolutionException(
                ResolutionFailureReason.NotFound,
                $"Dev Box '{id.ShortName}' has no RDP endpoint.");
        }

        return info;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DevBoxInfo>> ResolveAllAsync(string shortName, CancellationToken cancellationToken)
    {
        string token = await _authService.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://devboxes.azure.com/devboxes?api-version=2024-10-01-preview");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new DevBoxResolutionException(
                ResolutionFailureReason.ServiceUnreachable,
                "Could not reach the Dev Box service.",
                ex);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
            response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new DevBoxResolutionException(
                ResolutionFailureReason.Unauthorized,
                "You are not authorized to access Dev Box resources.");
        }

        response.EnsureSuccessStatusCode();

        DevBoxListResponse? listResponse = await response.Content
            .ReadFromJsonAsync<DevBoxListResponse>(s_jsonOptions, cancellationToken)
            .ConfigureAwait(false);

        if (listResponse?.Value is null)
        {
            return Array.Empty<DevBoxInfo>();
        }

        List<DevBoxInfo> results = [];

        foreach (DevBoxApiItem item in listResponse.Value)
        {
            if (!string.Equals(item.Name, shortName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            DevBoxState state = ParseState(item.PowerState);
            DevBoxEndpoint? endpoint = null;

            if (!string.IsNullOrEmpty(item.OsType) && state == DevBoxState.Running)
            {
                // The Dev Center API returns connection info via a separate call,
                // but for initial implementation we construct the endpoint from the URI.
                endpoint = new DevBoxEndpoint(item.Name + ".devbox.microsoft.com", 3389);
            }

            results.Add(new DevBoxInfo(item.Name ?? shortName, item.ProjectName ?? string.Empty, state, endpoint));
        }

        return results;
    }

    private static DevBoxState ParseState(string? powerState)
    {
        if (string.IsNullOrEmpty(powerState))
        {
            return DevBoxState.Unknown;
        }

        if (powerState.Contains("Running", StringComparison.OrdinalIgnoreCase))
        {
            return DevBoxState.Running;
        }

        if (powerState.Contains("Stopped", StringComparison.OrdinalIgnoreCase))
        {
            return DevBoxState.Stopped;
        }

        if (powerState.Contains("Deallocated", StringComparison.OrdinalIgnoreCase))
        {
            return DevBoxState.Deallocated;
        }

        return DevBoxState.Unknown;
    }

    // DTOs are instantiated by System.Text.Json deserialization.
#pragma warning disable CA1812
    private sealed record DevBoxListResponse(
        [property: JsonPropertyName("value")] DevBoxApiItem[]? Value);

    private sealed record DevBoxApiItem(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("projectName")] string? ProjectName,
        [property: JsonPropertyName("powerState")] string? PowerState,
        [property: JsonPropertyName("osType")] string? OsType);
#pragma warning restore CA1812
}
