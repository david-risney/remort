using System.Windows;
using System.Windows.Controls;

namespace Remort.Devices;

/// <summary>
/// Devices page showing all device cards.
/// </summary>
public partial class DevicesPage : Page
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DevicesPage"/> class.
    /// </summary>
    public DevicesPage()
    {
        InitializeComponent();
    }

    internal Task<AddDeviceDialogViewModel?> ShowAddDeviceDialogAsync()
    {
        var viewModel = new AddDeviceDialogViewModel();
        var dialog = new AddDeviceDialog
        {
            DataContext = viewModel,
            Owner = Window.GetWindow(this),
        };

        bool? result = dialog.ShowDialog();

        if (result == true)
        {
            return Task.FromResult<AddDeviceDialogViewModel?>(viewModel);
        }

        return Task.FromResult<AddDeviceDialogViewModel?>(null);
    }

    private void CardButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
    }

    private void MoreButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;

        if (sender is FrameworkElement element && element.ContextMenu is not null)
        {
            element.ContextMenu.PlacementTarget = element;
            element.ContextMenu.IsOpen = true;
        }
    }
}
