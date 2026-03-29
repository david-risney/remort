using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Remort.Devices;

namespace Remort.DeviceWindow;

/// <summary>
/// ViewModel for the per-device window. Owns the device reference, window title, and view toggle.
/// </summary>
public partial class DeviceWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _windowTitle;

    [ObservableProperty]
    private bool _isRdpViewActive;

    [ObservableProperty]
    private bool _isAutohideEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceWindowViewModel"/> class.
    /// </summary>
    /// <param name="device">The device this window represents.</param>
    public DeviceWindowViewModel(Device device)
    {
        ArgumentNullException.ThrowIfNull(device);

        Device = device;
        _windowTitle = device.Name;
        _isAutohideEnabled = device.GeneralSettings.AutohideTitleBar;
    }

    /// <summary>Gets the device this window represents.</summary>
    public Device Device { get; }

    /// <summary>
    /// Invokes the Windows Task View on the host machine via Win+Tab.
    /// </summary>
    [RelayCommand]
    private static void InvokeTaskView()
    {
        Interop.NativeMethods.SendTaskViewKeyPress();
    }

    /// <summary>
    /// Toggles between navigation view and RDP view.
    /// </summary>
    [RelayCommand]
    private void ToggleView()
    {
        IsRdpViewActive = !IsRdpViewActive;
    }
}
