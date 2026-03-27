using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Remort.Settings;

namespace Remort.Theme;

/// <summary>
/// ViewModel for the theme settings dialog. Exposes preset and custom profiles
/// for selection, with live preview and persistence.
/// </summary>
public partial class ThemeSettingsViewModel : ObservableObject
{
    private readonly IThemeService _themeService;
    private readonly ISettingsStore? _settingsStore;
    private readonly IProfileStore? _profileStore;
    private ColorProfile? _previousProfile;

    [ObservableProperty]
    private ColorProfile? _selectedProfile;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _newProfileName = string.Empty;

    [ObservableProperty]
    private string _nameValidationError = string.Empty;

    [ObservableProperty]
    private string _editPrimaryBackground = string.Empty;

    [ObservableProperty]
    private string _editSecondaryBackground = string.Empty;

    [ObservableProperty]
    private string _editAccent = string.Empty;

    [ObservableProperty]
    private string _editTextPrimary = string.Empty;

    [ObservableProperty]
    private string _editTextSecondary = string.Empty;

    [ObservableProperty]
    private string _editBorder = string.Empty;

    /// <summary>Gets or sets a value indicating whether the dialog should close.</summary>
    [ObservableProperty]
    private bool _shouldClose;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeSettingsViewModel"/> class.
    /// </summary>
    /// <param name="themeService">The theme service for applying profiles.</param>
    /// <param name="settingsStore">Optional settings store for persisting the active profile name.</param>
    /// <param name="profileStore">Optional profile store for loading/saving custom profiles.</param>
    public ThemeSettingsViewModel(IThemeService themeService, ISettingsStore? settingsStore = null, IProfileStore? profileStore = null)
    {
        _themeService = themeService;
        _settingsStore = settingsStore;
        _profileStore = profileStore;

        foreach (ColorProfile preset in PresetProfiles.All)
        {
            Profiles.Add(preset);
        }

        if (_profileStore is not null)
        {
            foreach (ColorProfile custom in _profileStore.LoadCustomProfiles())
            {
                Profiles.Add(custom);
            }
        }

        _selectedProfile = _themeService.ActiveProfile;
    }

    /// <summary>Gets the collection of available color profiles.</summary>
    public ObservableCollection<ColorProfile> Profiles { get; } = new();

    partial void OnSelectedProfileChanged(ColorProfile? value)
    {
        if (value is not null && !IsEditing)
        {
            _themeService.ApplyProfile(value);
            PersistActiveProfileName(value.Name);
        }
    }

    /// <summary>
    /// Closes the theme settings dialog.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        ShouldClose = true;
    }

    /// <summary>
    /// Creates a new custom color profile from the current default values.
    /// </summary>
    [RelayCommand]
    private void CreateProfile()
    {
        string name = NewProfileName.Trim();
        if (string.IsNullOrEmpty(name))
        {
            NameValidationError = "Profile name cannot be empty.";
            return;
        }

        if (Profiles.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            NameValidationError = "A profile with this name already exists.";
            return;
        }

        NameValidationError = string.Empty;

        ColorProfile template = PresetProfiles.Default;
        var newProfile = template with { Name = name, IsPreset = false };

        Profiles.Add(newProfile);
        SelectedProfile = newProfile;
        _previousProfile = _themeService.ActiveProfile;

        EditPrimaryBackground = newProfile.PrimaryBackground;
        EditSecondaryBackground = newProfile.SecondaryBackground;
        EditAccent = newProfile.Accent;
        EditTextPrimary = newProfile.TextPrimary;
        EditTextSecondary = newProfile.TextSecondary;
        EditBorder = newProfile.Border;

        IsEditing = true;
        NewProfileName = string.Empty;
    }

    /// <summary>
    /// Saves the current edit state to the selected custom profile.
    /// </summary>
    [RelayCommand]
    private void SaveProfile()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        var updated = new ColorProfile
        {
            Name = SelectedProfile.Name,
            IsPreset = false,
            PrimaryBackground = EditPrimaryBackground,
            SecondaryBackground = EditSecondaryBackground,
            Accent = EditAccent,
            TextPrimary = EditTextPrimary,
            TextSecondary = EditTextSecondary,
            Border = EditBorder,
        };

        int index = Profiles.IndexOf(SelectedProfile);
        if (index >= 0)
        {
            Profiles[index] = updated;
        }

        SelectedProfile = updated;
        _themeService.ApplyProfile(updated);
        PersistActiveProfileName(updated.Name);
        PersistCustomProfiles();

        IsEditing = false;
        _previousProfile = null;
    }

    /// <summary>
    /// Cancels the current edit and reverts to the previous profile.
    /// </summary>
    [RelayCommand]
    private void CancelEdit()
    {
        if (_previousProfile is not null)
        {
            _themeService.ApplyProfile(_previousProfile);

            // If a new unsaved profile was added, remove it
            if (SelectedProfile is not null && !SelectedProfile.IsPreset)
            {
                bool existsInStore = _profileStore?.LoadCustomProfiles()
                    .Any(p => string.Equals(p.Name, SelectedProfile.Name, StringComparison.OrdinalIgnoreCase)) ?? false;
                if (!existsInStore)
                {
                    Profiles.Remove(SelectedProfile);
                }
            }

            SelectedProfile = _previousProfile;
        }

        IsEditing = false;
        _previousProfile = null;
    }

    /// <summary>
    /// Opens the currently selected custom profile for editing.
    /// </summary>
    [RelayCommand]
    private void EditProfile()
    {
        if (SelectedProfile is null || SelectedProfile.IsPreset)
        {
            return;
        }

        _previousProfile = SelectedProfile;

        EditPrimaryBackground = SelectedProfile.PrimaryBackground;
        EditSecondaryBackground = SelectedProfile.SecondaryBackground;
        EditAccent = SelectedProfile.Accent;
        EditTextPrimary = SelectedProfile.TextPrimary;
        EditTextSecondary = SelectedProfile.TextSecondary;
        EditBorder = SelectedProfile.Border;

        IsEditing = true;
    }

    /// <summary>
    /// Deletes the currently selected custom profile after confirmation.
    /// </summary>
    [RelayCommand]
    private void DeleteProfile()
    {
        if (SelectedProfile is null || SelectedProfile.IsPreset)
        {
            return;
        }

        // Confirmation is handled by the View before calling this command
        ColorProfile toDelete = SelectedProfile;
        bool wasActive = string.Equals(_themeService.ActiveProfile.Name, toDelete.Name, StringComparison.OrdinalIgnoreCase);

        Profiles.Remove(toDelete);
        PersistCustomProfiles();

        if (wasActive)
        {
            SelectedProfile = PresetProfiles.Default;
            _themeService.ApplyProfile(PresetProfiles.Default);
            PersistActiveProfileName(PresetProfiles.Default.Name);
        }
        else if (Profiles.Count > 0)
        {
            SelectedProfile = Profiles[0];
        }
    }

    /// <summary>
    /// Applies a live preview from the current edit color values.
    /// </summary>
    private void ApplyEditPreview()
    {
        if (!IsEditing)
        {
            return;
        }

        try
        {
            var preview = new ColorProfile
            {
                Name = SelectedProfile?.Name ?? "Preview",
                IsPreset = false,
                PrimaryBackground = EditPrimaryBackground,
                SecondaryBackground = EditSecondaryBackground,
                Accent = EditAccent,
                TextPrimary = EditTextPrimary,
                TextSecondary = EditTextSecondary,
                Border = EditBorder,
            };

            _themeService.ApplyProfile(preview);
        }
#pragma warning disable CA1031 // Do not catch general exception types — invalid hex colors during editing are expected
        catch
#pragma warning restore CA1031
        {
            // Invalid hex color during editing; ignore until a valid value is entered.
        }
    }

    partial void OnEditPrimaryBackgroundChanged(string value) => ApplyEditPreview();

    partial void OnEditSecondaryBackgroundChanged(string value) => ApplyEditPreview();

    partial void OnEditAccentChanged(string value) => ApplyEditPreview();

    partial void OnEditTextPrimaryChanged(string value) => ApplyEditPreview();

    partial void OnEditTextSecondaryChanged(string value) => ApplyEditPreview();

    partial void OnEditBorderChanged(string value) => ApplyEditPreview();

    private void PersistActiveProfileName(string name)
    {
        if (_settingsStore is null)
        {
            return;
        }

        AppSettings settings = _settingsStore.Load();
        _settingsStore.Save(settings with { ActiveProfileName = name });
    }

    private void PersistCustomProfiles()
    {
        if (_profileStore is null)
        {
            return;
        }

        var customProfiles = Profiles.Where(p => !p.IsPreset).ToList();
        _profileStore.SaveCustomProfiles(customProfiles);
    }
}
