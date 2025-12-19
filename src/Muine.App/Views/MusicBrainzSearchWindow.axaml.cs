using Avalonia.Controls;
using Avalonia.Interactivity;
using Muine.App.ViewModels;
using System.ComponentModel;

namespace Muine.App.Views;

public partial class MusicBrainzSearchWindow : Window
{
    public MusicBrainzSearchWindow()
    {
        InitializeComponent();
        
        // Subscribe to DataContext changes to monitor DialogResult
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        // Unsubscribe from old ViewModel
        if (sender is Window window && window.DataContext is MusicBrainzSearchViewModel oldViewModel)
        {
            oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
        
        // Subscribe to new ViewModel
        if (DataContext is MusicBrainzSearchViewModel newViewModel)
        {
            newViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Close the dialog when DialogResult is set
        if (e.PropertyName == nameof(MusicBrainzSearchViewModel.DialogResult))
        {
            if (DataContext is MusicBrainzSearchViewModel viewModel)
            {
                Close(viewModel.DialogResult);
            }
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
    
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Unsubscribe when closing
        if (DataContext is MusicBrainzSearchViewModel viewModel)
        {
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
        
        base.OnClosing(e);
    }
}
