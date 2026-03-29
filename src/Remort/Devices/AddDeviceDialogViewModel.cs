using CommunityToolkit.Mvvm.ComponentModel;

namespace Remort.Devices;

/// <summary>
/// ViewModel for the Add Device dialog.
/// </summary>
public partial class AddDeviceDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _hostname = string.Empty;

    [ObservableProperty]
    private string _validationError = string.Empty;

    /// <summary>Gets a value indicating whether the dialog was confirmed.</summary>
    public bool IsConfirmed { get; private set; }

    /// <summary>Gets a value indicating whether both fields have content.</summary>
    public bool CanConfirm => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Hostname);

    /// <summary>
    /// Validates the inputs and marks the dialog as confirmed if valid.
    /// </summary>
    /// <returns><see langword="true"/> if validation passed; <see langword="false"/> otherwise.</returns>
    public bool TryConfirm()
    {
        string trimmedName = Name.Trim();
        string trimmedHostname = Hostname.Trim();

        if (string.IsNullOrEmpty(trimmedName))
        {
            ValidationError = "Name is required.";
            return false;
        }

        if (string.IsNullOrEmpty(trimmedHostname))
        {
            ValidationError = "Hostname is required.";
            return false;
        }

        Name = trimmedName;
        Hostname = trimmedHostname;
        ValidationError = string.Empty;
        IsConfirmed = true;
        return true;
    }

    /// <summary>
    /// Creates a new <see cref="Device"/> from the confirmed dialog inputs.
    /// </summary>
    /// <returns>A new device with default settings.</returns>
    /// <exception cref="InvalidOperationException">The dialog was not confirmed.</exception>
    public Device CreateDevice()
    {
        if (!IsConfirmed)
        {
            throw new InvalidOperationException("Cannot create device before dialog is confirmed.");
        }

        return new Device { Name = Name, Hostname = Hostname };
    }

    partial void OnNameChanged(string value)
    {
        OnPropertyChanged(nameof(CanConfirm));
    }

    partial void OnHostnameChanged(string value)
    {
        OnPropertyChanged(nameof(CanConfirm));
    }
}
