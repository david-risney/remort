using System.Windows.Threading;

namespace Remort.VirtualDesktop;

/// <summary>
/// Polls <see cref="IVirtualDesktopService.IsOnCurrentDesktop"/> at a fixed interval
/// and raises <see cref="IDesktopSwitchDetector.SwitchedToDesktop"/> on false→true transitions.
/// </summary>
public sealed class DesktopSwitchDetector : IDesktopSwitchDetector
{
    private static readonly TimeSpan s_pollInterval = TimeSpan.FromMilliseconds(500);

    private readonly IVirtualDesktopService _virtualDesktopService;
    private DispatcherTimer? _timer;
    private IntPtr _hwnd;
    private bool _wasOnCurrentDesktop = true;
    private bool _isMonitoring;

    /// <summary>
    /// Initializes a new instance of the <see cref="DesktopSwitchDetector"/> class.
    /// </summary>
    /// <param name="virtualDesktopService">The virtual desktop service used to query window visibility.</param>
    public DesktopSwitchDetector(IVirtualDesktopService virtualDesktopService)
    {
        _virtualDesktopService = virtualDesktopService;
    }

    /// <inheritdoc/>
    public event EventHandler? SwitchedToDesktop;

    /// <inheritdoc/>
    public void StartMonitoring(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        StopMonitoring();

        _hwnd = hwnd;
        _wasOnCurrentDesktop = _virtualDesktopService.IsOnCurrentDesktop(_hwnd);
        _isMonitoring = true;

        _timer = new DispatcherTimer { Interval = s_pollInterval };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    /// <inheritdoc/>
    public void StopMonitoring()
    {
        _isMonitoring = false;

        if (_timer is not null)
        {
            _timer.Tick -= OnTimerTick;
            _timer.Stop();
            _timer = null;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        StopMonitoring();
    }

    /// <summary>
    /// Drives one poll cycle synchronously. Used by unit tests to simulate a timer tick
    /// without requiring a dispatcher message pump.
    /// </summary>
    internal void SimulateTick()
    {
        OnTimerTick(null, EventArgs.Empty);
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (!_isMonitoring)
        {
            return;
        }

        bool isOnCurrentDesktop = _virtualDesktopService.IsOnCurrentDesktop(_hwnd);

        if (!_wasOnCurrentDesktop && isOnCurrentDesktop)
        {
            SwitchedToDesktop?.Invoke(this, EventArgs.Empty);
        }

        _wasOnCurrentDesktop = isOnCurrentDesktop;
    }
}
