using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Muine.App.Views;

public partial class MusicBrainzSearchWindow : Window
{
    public MusicBrainzSearchWindow()
    {
        InitializeComponent();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
