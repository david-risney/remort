using System.Diagnostics;
using System.IO;
using System.Net.Sockets;

namespace Remort.Tests.EndToEnd;

/// <summary>
/// Fixture that ensures a local RDP server is available for E2E tests.
/// Supports two modes:
/// <list type="bullet">
///   <item>Windows loopback RDP (localhost:3389) — requires RDP enabled on the machine</item>
///   <item>FreeRDP shadow server — started automatically if wfreerdp-shadow-cli is found</item>
/// </list>
/// Set the environment variable <c>REMORT_E2E_RDP_HOST</c> to override the target
/// (e.g., <c>localhost</c> or <c>127.0.0.1:13389</c>).
/// </summary>
public sealed class LocalRdpServerFixture : IAsyncLifetime
{
    private const int DefaultRdpPort = 3389;
    private const int FreeRdpPort = 13389;
    private const string EnvVarHost = "REMORT_E2E_RDP_HOST";

    private Process? _serverProcess;

    /// <summary>
    /// Gets the hostname (or IP) to connect to.
    /// </summary>
    public string Host { get; private set; } = "127.0.0.1";

    /// <summary>
    /// Gets the port the RDP server is listening on.
    /// </summary>
    public int Port { get; private set; } = DefaultRdpPort;

    /// <summary>
    /// Gets a value indicating whether a usable RDP endpoint was found.
    /// </summary>
    public bool IsAvailable { get; private set; }

    /// <summary>
    /// Gets a description of why the server is unavailable (empty if available).
    /// </summary>
    public string SkipReason { get; private set; } = string.Empty;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        // 1. Explicit override via environment variable
        string? envHost = Environment.GetEnvironmentVariable(EnvVarHost);
        if (!string.IsNullOrWhiteSpace(envHost))
        {
            ParseHostPort(envHost);
            IsAvailable = await IsPortOpenAsync(Host, Port).ConfigureAwait(false);
            if (!IsAvailable)
            {
                SkipReason = $"REMORT_E2E_RDP_HOST={envHost} is set but port {Port} is not reachable.";
            }

            return;
        }

        // 2. Try Windows loopback RDP (localhost:3389)
        if (await IsPortOpenAsync("127.0.0.1", DefaultRdpPort).ConfigureAwait(false))
        {
            Host = "127.0.0.1";
            Port = DefaultRdpPort;
            IsAvailable = true;
            return;
        }

        // 3. Try starting FreeRDP shadow server
        string? freeRdpPath = FindFreeRdpShadow();
        if (freeRdpPath is not null)
        {
            _serverProcess = StartFreeRdpShadow(freeRdpPath);
            if (_serverProcess is not null)
            {
                // Give it time to start listening
                for (int i = 0; i < 10; i++)
                {
                    await Task.Delay(500).ConfigureAwait(false);
                    if (await IsPortOpenAsync("127.0.0.1", FreeRdpPort).ConfigureAwait(false))
                    {
                        Host = "127.0.0.1";
                        Port = FreeRdpPort;
                        IsAvailable = true;
                        return;
                    }
                }

                SkipReason = $"FreeRDP shadow server started but port {FreeRdpPort} never became reachable.";
                return;
            }
        }

        SkipReason = "No local RDP server available. Enable Windows RDP, install FreeRDP, or set REMORT_E2E_RDP_HOST.";
    }

    /// <inheritdoc/>
    public Task DisposeAsync()
    {
        if (_serverProcess is not null && !_serverProcess.HasExited)
        {
            _serverProcess.Kill(entireProcessTree: true);
            _serverProcess.Dispose();
        }

        return Task.CompletedTask;
    }

    private static async Task<bool> IsPortOpenAsync(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await client.ConnectAsync(host, port, cts.Token).ConfigureAwait(false);
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private static string? FindFreeRdpShadow()
    {
        string[] candidates =
        [
            "wfreerdp-shadow-cli.exe",
            "wfreerdp-shadow.exe",
        ];

        foreach (string name in candidates)
        {
            // Check PATH
            try
            {
                var psi = new ProcessStartInfo("where.exe", name)
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var proc = Process.Start(psi);
                string? output = proc?.StandardOutput.ReadLine();
                proc?.WaitForExit(2000);
                if (!string.IsNullOrWhiteSpace(output) && File.Exists(output))
                {
                    return output;
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types — search is best-effort
            catch (Exception)
#pragma warning restore CA1031
            {
                // Ignore search failures
            }
        }

        return null;
    }

    private static Process? StartFreeRdpShadow(string path)
    {
        try
        {
            var psi = new ProcessStartInfo(path, $"/port:{FreeRdpPort} /sec:nla /sam-file:none")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            return Process.Start(psi);
        }
#pragma warning disable CA1031 // Do not catch general exception types — server start is best-effort
        catch (Exception)
#pragma warning restore CA1031
        {
            return null;
        }
    }

    private void ParseHostPort(string hostPort)
    {
        // Supports "host", "host:port", "127.0.0.1:13389"
        int colonIdx = hostPort.LastIndexOf(':');
        if (colonIdx > 0 && int.TryParse(hostPort[(colonIdx + 1)..], out int port))
        {
            Host = hostPort[..colonIdx];
            Port = port;
        }
        else
        {
            Host = hostPort;
            Port = DefaultRdpPort;
        }
    }
}
