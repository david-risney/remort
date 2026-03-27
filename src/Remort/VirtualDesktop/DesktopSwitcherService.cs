using System.Windows.Threading;
using Microsoft.Win32;
using Remort.Interop;

namespace Remort.VirtualDesktop;

/// <summary>
/// Enumerates virtual desktops from the Windows Registry and switches
/// between them by simulating Win+Ctrl+Arrow keyboard shortcuts via SendInput.
/// </summary>
public sealed class DesktopSwitcherService : IDesktopSwitcherService
{
    private const string VirtualDesktopRegistryPath =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VirtualDesktops";

    private const string DesktopsSubkeyPath =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VirtualDesktops\Desktops";

    private const int GuidByteLength = 16;

    private static readonly TimeSpan s_pollInterval = TimeSpan.FromMilliseconds(500);

    private DispatcherTimer? _timer;
    private IReadOnlyList<VirtualDesktopInfo> _cachedDesktops = [];
    private int _cachedCurrentIndex = -1;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DesktopSwitcherService"/> class.
    /// Checks the registry to determine if virtual desktop enumeration is supported.
    /// </summary>
    public DesktopSwitcherService()
    {
        IsSupported = CheckRegistrySupported();
    }

    /// <inheritdoc/>
    public event EventHandler? DesktopsChanged;

    /// <inheritdoc/>
    public bool IsSupported { get; }

    /// <inheritdoc/>
    public IReadOnlyList<VirtualDesktopInfo> GetDesktops()
    {
        if (!IsSupported)
        {
            return [];
        }

        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(VirtualDesktopRegistryPath);
        if (key is null)
        {
            return [];
        }

        if (key.GetValue("VirtualDesktopIDs") is not byte[] idsBytes || idsBytes.Length < GuidByteLength)
        {
            return [];
        }

        int count = idsBytes.Length / GuidByteLength;
        var desktops = new List<VirtualDesktopInfo>(count);

        for (int i = 0; i < count; i++)
        {
            var guidBytes = new byte[GuidByteLength];
            Buffer.BlockCopy(idsBytes, i * GuidByteLength, guidBytes, 0, GuidByteLength);
            var id = new Guid(guidBytes);

            string name = ResolveDesktopName(id, i);
            desktops.Add(new VirtualDesktopInfo(id, name, i));
        }

        return desktops;
    }

    /// <inheritdoc/>
    public int GetCurrentDesktopIndex()
    {
        if (!IsSupported)
        {
            return -1;
        }

        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(VirtualDesktopRegistryPath);
        if (key is null)
        {
            return -1;
        }

        if (key.GetValue("CurrentVirtualDesktop") is not byte[] currentBytes || currentBytes.Length < GuidByteLength)
        {
            return -1;
        }

        var currentId = new Guid(currentBytes);

        if (key.GetValue("VirtualDesktopIDs") is not byte[] idsBytes || idsBytes.Length < GuidByteLength)
        {
            return -1;
        }

        int count = idsBytes.Length / GuidByteLength;
        for (int i = 0; i < count; i++)
        {
            var guidBytes = new byte[GuidByteLength];
            Buffer.BlockCopy(idsBytes, i * GuidByteLength, guidBytes, 0, GuidByteLength);
            var id = new Guid(guidBytes);

            if (id == currentId)
            {
                return i;
            }
        }

        return -1;
    }

    /// <inheritdoc/>
    public void SwitchToDesktop(int targetIndex, int currentIndex)
    {
        if (targetIndex == currentIndex)
        {
            return;
        }

        if (targetIndex < 0 || currentIndex < 0)
        {
            return;
        }

        int delta = targetIndex - currentIndex;
        ushort arrowKey = delta > 0 ? NativeMethods.VK_RIGHT : NativeMethods.VK_LEFT;
        int steps = Math.Abs(delta);

        for (int i = 0; i < steps; i++)
        {
            if (i > 0)
            {
                Thread.Sleep(50);
            }

            SendDesktopSwitchKeys(arrowKey);
        }
    }

    /// <inheritdoc/>
    public void StartMonitoring()
    {
        StopMonitoring();

        _cachedDesktops = GetDesktops();
        _cachedCurrentIndex = GetCurrentDesktopIndex();

        _timer = new DispatcherTimer { Interval = s_pollInterval };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    /// <inheritdoc/>
    public void StopMonitoring()
    {
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
        if (!_disposed)
        {
            StopMonitoring();
            _disposed = true;
        }
    }

    private static bool CheckRegistrySupported()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(VirtualDesktopRegistryPath);
        if (key is null)
        {
            return false;
        }

        byte[]? idsBytes = key.GetValue("VirtualDesktopIDs") as byte[];
        return idsBytes is not null && idsBytes.Length >= GuidByteLength;
    }

    private static string ResolveDesktopName(Guid desktopId, int index)
    {
        string subkeyPath = $@"{DesktopsSubkeyPath}\{{{desktopId}}}";
        using RegistryKey? desktopKey = Registry.CurrentUser.OpenSubKey(subkeyPath);

        if (desktopKey is not null)
        {
            string? name = desktopKey.GetValue("Name") as string;
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }
        }

        return $"Desktop {index + 1}";
    }

    private static void SendDesktopSwitchKeys(ushort arrowKey)
    {
        int size = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>();

        var inputs = new NativeMethods.INPUT[]
        {
            MakeKeyDown(NativeMethods.VK_LWIN),
            MakeKeyDown(NativeMethods.VK_LCONTROL),
            MakeKeyDown(arrowKey),
            MakeKeyUp(arrowKey),
            MakeKeyUp(NativeMethods.VK_LCONTROL),
            MakeKeyUp(NativeMethods.VK_LWIN),
        };

        _ = NativeMethods.SendInput((uint)inputs.Length, inputs, size);
    }

    private static NativeMethods.INPUT MakeKeyDown(ushort vk)
    {
        return new NativeMethods.INPUT
        {
            Type = NativeMethods.INPUT_KEYBOARD,
            Union = new NativeMethods.INPUTUNION
            {
                Keyboard = new NativeMethods.KEYBDINPUT
                {
                    VirtualKey = vk,
                },
            },
        };
    }

    private static NativeMethods.INPUT MakeKeyUp(ushort vk)
    {
        return new NativeMethods.INPUT
        {
            Type = NativeMethods.INPUT_KEYBOARD,
            Union = new NativeMethods.INPUTUNION
            {
                Keyboard = new NativeMethods.KEYBDINPUT
                {
                    VirtualKey = vk,
                    Flags = NativeMethods.KEYEVENTF_KEYUP,
                },
            },
        };
    }

    private static bool DesktopListsEqual(
        IReadOnlyList<VirtualDesktopInfo> a,
        IReadOnlyList<VirtualDesktopInfo> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }

        return true;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        IReadOnlyList<VirtualDesktopInfo> currentDesktops = GetDesktops();
        int currentIndex = GetCurrentDesktopIndex();

        bool changed = currentIndex != _cachedCurrentIndex
            || !DesktopListsEqual(_cachedDesktops, currentDesktops);

        if (changed)
        {
            _cachedDesktops = currentDesktops;
            _cachedCurrentIndex = currentIndex;
            DesktopsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
