using Avalonia.Controls;
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
}