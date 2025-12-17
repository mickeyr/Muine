using Avalonia.Controls;
using Avalonia.Input;
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
            
            // Handle double-click on online station to add to library
            if (OnlineStationsGrid != null)
            {
                OnlineStationsGrid.DoubleTapped += async (sender, args) =>
                {
                    if (DataContext is RadioViewModel vm && vm.SelectedOnlineStation != null)
                    {
                        await vm.AddOnlineStationToLibraryAsync(vm.SelectedOnlineStation);
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

    private async void OnSearchOnlineClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is RadioViewModel vm)
        {
            await vm.SearchOnlineAsync();
        }
    }

    private void OnClearOnlineClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is RadioViewModel vm)
        {
            vm.ClearOnlineSearch();
        }
    }

    private async void OnAddOnlineStationClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is RadioViewModel vm && vm.SelectedOnlineStation != null)
        {
            await vm.AddOnlineStationToLibraryAsync(vm.SelectedOnlineStation);
        }
    }

    private void OnPlayOnlineStationClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is RadioViewModel vm && vm.SelectedOnlineStation != null)
        {
            StationDoubleClicked?.Invoke(this, vm.SelectedOnlineStation);
        }
    }

    private async void OnOnlineSearchBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is RadioViewModel vm)
        {
            await vm.SearchOnlineAsync();
        }
    }
}
