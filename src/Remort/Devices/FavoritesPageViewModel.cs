using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Remort.Devices;

/// <summary>
/// ViewModel for the Favorites page — shows only favorite devices.
/// </summary>
public partial class FavoritesPageViewModel : ObservableObject
{
    private readonly IDeviceStore _deviceStore;
    private readonly IDeviceWindowManager _deviceWindowManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="FavoritesPageViewModel"/> class.
    /// </summary>
    /// <param name="deviceStore">The device store.</param>
    /// <param name="deviceWindowManager">The device window manager.</param>
    public FavoritesPageViewModel(IDeviceStore deviceStore, IDeviceWindowManager deviceWindowManager)
    {
        _deviceStore = deviceStore;
        _deviceWindowManager = deviceWindowManager;

        Refresh();

        _deviceStore.DeviceAdded += (_, _) => Refresh();
        _deviceStore.DeviceUpdated += (_, _) => Refresh();
        _deviceStore.DeviceRemoved += (_, _) => Refresh();
    }

    /// <summary>Gets the collection of favorite device card ViewModels.</summary>
    public ObservableCollection<DeviceCardViewModel> Devices { get; } = [];

    /// <summary>Gets a value indicating whether there are any favorites.</summary>
    public bool HasFavorites => Devices.Count > 0;

    /// <summary>Gets a value indicating whether there are no favorites (for empty state).</summary>
    public bool HasNoFavorites => Devices.Count == 0;

    private void Refresh()
    {
        Devices.Clear();
        foreach (Device device in _deviceStore.GetAll())
        {
            if (device.IsFavorite)
            {
                Devices.Add(new DeviceCardViewModel(device, _deviceStore, _deviceWindowManager));
            }
        }

        OnPropertyChanged(nameof(HasFavorites));
        OnPropertyChanged(nameof(HasNoFavorites));
    }
}
