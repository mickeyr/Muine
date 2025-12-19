using Avalonia.Controls;
using Avalonia.Interactivity;
using Muine.App.ViewModels;
using Muine.Core.Models;
using System.Linq;

namespace Muine.App.Views;

public partial class MetadataReviewWindow : Window
{
    public MetadataReviewWindow()
    {
        InitializeComponent();
    }

    private async void OnSearchMusicBrainzClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MetadataReviewViewModel viewModel)
            return;

        // Get the song from the button's data context (the row)
        if (sender is Button button && button.DataContext is Song song)
        {
            // Open MusicBrainz search dialog
            var dialog = new MusicBrainzSearchWindow
            {
                DataContext = App.CreateMusicBrainzSearchViewModel()
            };

            if (dialog.DataContext is MusicBrainzSearchViewModel searchViewModel)
            {
                searchViewModel.Initialize(song);
                await dialog.ShowDialog(this);

                // If metadata was applied, remove from review list
                if (searchViewModel.DialogResult)
                {
                    viewModel.RemoveSongFromReview(song);
                }
            }
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
