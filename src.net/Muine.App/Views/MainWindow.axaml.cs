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
            await viewModel.ImportMusicFolderCommand.ExecuteAsync(StorageProvider);
        }
    }

    private async void OnAddMusicFilesClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.AddMusicFilesCommand.ExecuteAsync(StorageProvider);
        }
    }
}