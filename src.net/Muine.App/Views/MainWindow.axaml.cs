using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Muine.App.ViewModels;

namespace Muine.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnImportMusicFolderClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            try
            {
                await viewModel.ImportMusicFolderCommand.ExecuteAsync(StorageProvider);
            }
            catch
            {
                // ViewModel handles error display via StatusMessage
            }
        }
    }

    private async void OnAddMusicFilesClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            try
            {
                await viewModel.AddMusicFilesCommand.ExecuteAsync(StorageProvider);
            }
            catch
            {
                // ViewModel handles error display via StatusMessage
            }
        }
    }

    private async void OnSongDoubleClick(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel && viewModel.SelectedSong != null)
        {
            try
            {
                await viewModel.PlaySelectedSongCommand.ExecuteAsync(null);
            }
            catch
            {
                // ViewModel handles error display via StatusMessage
            }
        }
    }

    private void OnSliderPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Slider.ValueProperty && sender is Slider slider && DataContext is MainWindowViewModel viewModel)
        {
            // Only seek if the user is manually changing the slider, not if it's updating from playback
            if (slider.IsPointerOver && viewModel.IsPlaying)
            {
                viewModel.SeekCommand.Execute(slider.Value);
            }
        }
    }
}