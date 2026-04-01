using System.Windows;
using System.Windows.Controls;

namespace Remort.Devices;

/// <summary>
/// Favorites page showing only favorite device cards.
/// </summary>
public partial class FavoritesPage : Page
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FavoritesPage"/> class.
    /// </summary>
    public FavoritesPage()
    {
        InitializeComponent();
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
