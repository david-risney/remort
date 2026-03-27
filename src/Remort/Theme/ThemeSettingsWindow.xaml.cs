using System.ComponentModel;
using System.Windows;

namespace Remort.Theme;

/// <summary>
/// Code-behind for the Theme Settings dialog window.
/// </summary>
public partial class ThemeSettingsWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeSettingsWindow"/> class.
    /// </summary>
    public ThemeSettingsWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    /// <inheritdoc/>
    protected override void OnClosing(CancelEventArgs e)
    {
        if (DataContext is ThemeSettingsViewModel vm)
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;
        }

        base.OnClosing(e);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ThemeSettingsViewModel oldVm)
        {
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (e.NewValue is ThemeSettingsViewModel newVm)
        {
            newVm.PropertyChanged += OnViewModelPropertyChanged;

            // Enable/disable Edit and Delete buttons based on selection
            UpdateEditDeleteVisibility(newVm);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not ThemeSettingsViewModel vm)
        {
            return;
        }

        if (e.PropertyName == nameof(ThemeSettingsViewModel.ShouldClose) && vm.ShouldClose)
        {
            Close();
        }
        else if (e.PropertyName == nameof(ThemeSettingsViewModel.SelectedProfile))
        {
            UpdateEditDeleteVisibility(vm);
        }
    }

    private void UpdateEditDeleteVisibility(ThemeSettingsViewModel vm)
    {
        bool isCustom = vm.SelectedProfile is not null && !vm.SelectedProfile.IsPreset;
        EditButton.IsEnabled = isCustom;
        DeleteButton.IsEnabled = isCustom;
    }
}
