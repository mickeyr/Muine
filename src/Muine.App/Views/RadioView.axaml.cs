using Avalonia.Controls;
using Avalonia.Interactivity;
using Muine.App.ViewModels;
using Muine.Core.Models;
using System;

namespace Muine.App.Views;

public partial class RadioView : UserControl
{
    public event EventHandler<RadioStation>? StationDoubleClicked;
    public event EventHandler? AddStationRequested;
    public event EventHandler<RadioStation>? EditStationRequested;
    public event EventHandler? RefreshRequested;

    public RadioView()
    {
        InitializeComponent();
        
        // Handle double-click on station to play
        this.AttachedToVisualTree += (s, e) =>
        {
            if (StationsGrid != null)
            {
                StationsGrid.DoubleTapped += (sender, args) =>
                {
                    if (DataContext is RadioViewModel vm && vm.SelectedStation != null)
                    {
                        StationDoubleClicked?.Invoke(this, vm.SelectedStation);
                    }
                };
            }
        };
    }

    private void OnAddStationClick(object? sender, RoutedEventArgs e)
    {
        AddStationRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnEditStationClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is RadioViewModel vm && vm.SelectedStation != null)
        {
            EditStationRequested?.Invoke(this, vm.SelectedStation);
        }
    }

    private void OnPlayStationClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is RadioViewModel vm && vm.SelectedStation != null)
        {
            StationDoubleClicked?.Invoke(this, vm.SelectedStation);
        }
    }

    private void OnDeleteStationClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is RadioViewModel vm && vm.SelectedStation != null)
        {
            _ = vm.DeleteStationAsync(vm.SelectedStation);
        }
    }

    private async void OnSearchClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is RadioViewModel vm)
        {
            await vm.SearchStationsAsync();
        }
    }

    private void OnRefreshClick(object? sender, RoutedEventArgs e)
    {
        RefreshRequested?.Invoke(this, EventArgs.Empty);
    }
}
