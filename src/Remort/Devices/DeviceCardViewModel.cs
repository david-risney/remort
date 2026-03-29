using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace Remort.Devices;

/// <summary>
/// ViewModel for a single device card in the Devices/Favorites list.
/// </summary>
public partial class DeviceCardViewModel : ObservableObject
{
    private static readonly LinearGradientBrush[] s_gradients =
    [
        CreateGradient("#FF6B6B", "#C44569"),
        CreateGradient("#4ECDC4", "#2C3E50"),
        CreateGradient("#F7DC6F", "#F0932B"),
        CreateGradient("#686DE0", "#30336B"),
        CreateGradient("#6AB04C", "#27AE60"),
        CreateGradient("#E056A0", "#8E44AD"),
        CreateGradient("#22A6B3", "#1289A7"),
        CreateGradient("#F8C291", "#E55039"),
    ];

    private readonly IDeviceStore _deviceStore;
    private readonly IDeviceWindowManager _deviceWindowManager;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private Brush _backgroundBrush;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceCardViewModel"/> class.
    /// </summary>
    /// <param name="device">The device to display.</param>
    /// <param name="deviceStore">The device store for persisting changes.</param>
    /// <param name="deviceWindowManager">The window manager for opening device windows.</param>
    public DeviceCardViewModel(Device device, IDeviceStore deviceStore, IDeviceWindowManager deviceWindowManager)
    {
        ArgumentNullException.ThrowIfNull(device);

        Device = device;
        _deviceStore = deviceStore;
        _deviceWindowManager = deviceWindowManager;
        _name = device.Name;
        _isFavorite = device.IsFavorite;
        _backgroundBrush = LoadBackground(device);
    }

    /// <summary>Gets the underlying device.</summary>
    public Device Device { get; private set; }

    /// <summary>
    /// Updates this card from a changed device instance.
    /// </summary>
    /// <param name="device">The updated device.</param>
    public void Refresh(Device device)
    {
        ArgumentNullException.ThrowIfNull(device);

        Device = device;
        Name = device.Name;
        IsFavorite = device.IsFavorite;
        BackgroundBrush = LoadBackground(device);
    }

    private static Brush LoadBackground(Device device)
    {
        if (!string.IsNullOrEmpty(device.LastScreenshotPath))
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string fullPath = Path.Combine(appData, "Remort", device.LastScreenshotPath);

                if (File.Exists(fullPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return new ImageBrush(bitmap) { Stretch = Stretch.UniformToFill };
                }
            }
#pragma warning disable CA1031 // Fall back to gradient on any image load error
            catch (Exception)
#pragma warning restore CA1031
            {
                // Corrupt or missing screenshot — use gradient fallback.
            }
        }

        // Assign gradient deterministically from device Id hash.
        int index = Math.Abs(device.Id.GetHashCode()) % s_gradients.Length;
        return s_gradients[index];
    }

    private static LinearGradientBrush CreateGradient(string startHex, string endHex)
    {
        var brush = new LinearGradientBrush(
            (Color)ColorConverter.ConvertFromString(startHex),
            (Color)ColorConverter.ConvertFromString(endHex),
            45.0);
        brush.Freeze();
        return brush;
    }

    [RelayCommand]
    private void Open()
    {
        _deviceWindowManager.OpenOrFocus(Device);
    }

    [RelayCommand]
    private void ToggleFavorite()
    {
        Device updated = Device with { IsFavorite = !Device.IsFavorite };
        _deviceStore.Update(updated);
        _deviceStore.Save();
    }

    [RelayCommand]
    private void Remove()
    {
        _deviceWindowManager.CloseForDevice(Device.Id);
        _deviceStore.Remove(Device.Id);
        _deviceStore.Save();
    }
}
