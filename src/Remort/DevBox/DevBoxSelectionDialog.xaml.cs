using System.Windows;

namespace Remort.DevBox;

/// <summary>
/// Dialog for disambiguating multiple Dev Box matches.
/// </summary>
public partial class DevBoxSelectionDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DevBoxSelectionDialog"/> class.
    /// </summary>
    /// <param name="candidates">The list of matching Dev Boxes to display.</param>
    public DevBoxSelectionDialog(IReadOnlyList<DevBoxInfo> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        InitializeComponent();

        var items = new List<DevBoxDisplayItem>(candidates.Count);
        for (int i = 0; i < candidates.Count; i++)
        {
            items.Add(new DevBoxDisplayItem(candidates[i]));
        }

        DevBoxListBox.ItemsSource = items;
        DevBoxListBox.SelectedIndex = 0;
    }

    /// <summary>
    /// Gets the Dev Box selected by the user, or <see langword="null"/> if cancelled.
    /// </summary>
    public DevBoxInfo? SelectedDevBox { get; private set; }

    private void OnConnectClick(object sender, RoutedEventArgs e)
    {
        if (DevBoxListBox.SelectedItem is DevBoxDisplayItem item)
        {
            SelectedDevBox = item.Info;
            DialogResult = true;
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private sealed class DevBoxDisplayItem
    {
        public DevBoxDisplayItem(DevBoxInfo info)
        {
            Info = info;
            DisplayText = $"{info.Name} ({info.ProjectName}) \u2014 {info.State}";
        }

        public DevBoxInfo Info { get; }

        public string DisplayText { get; }
    }
}
