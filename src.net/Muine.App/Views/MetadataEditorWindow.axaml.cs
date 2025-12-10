using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Muine.App.ViewModels;

namespace Muine.App.Views;

public partial class MetadataEditorWindow : Window
{
    public MetadataEditorWindow()
    {
        InitializeComponent();
    }

    private async void OnSelectCoverImageClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MetadataEditorViewModel viewModel)
        {
            await viewModel.SelectCoverImageCommand.ExecuteAsync(StorageProvider);
        }
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MetadataEditorViewModel viewModel)
        {
            // Execute the save command and check if it succeeded
            await viewModel.SaveCommand.ExecuteAsync(null);
            if (!viewModel.HasChanges)
            {
                Close(true);
            }
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
