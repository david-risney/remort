using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Remort.Devices;

/// <summary>
/// Modal dialog for adding a new device.
/// </summary>
public partial class AddDeviceDialog : FluentWindow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddDeviceDialog"/> class.
    /// </summary>
    public AddDeviceDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => NameTextBox.Focus();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        TryConfirmAndClose();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void TextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;

        // If Name is empty, focus Name
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            NameTextBox.Focus();
            return;
        }

        // If Hostname is empty, focus Hostname
        if (string.IsNullOrWhiteSpace(HostnameTextBox.Text))
        {
            HostnameTextBox.Focus();
            return;
        }

        // Both filled — submit
        TryConfirmAndClose();
    }

    private void TryConfirmAndClose()
    {
        if (DataContext is AddDeviceDialogViewModel vm && vm.TryConfirm())
        {
            DialogResult = true;
            Close();
        }
    }
}
