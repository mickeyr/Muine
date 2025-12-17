using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Muine.Core.Models;
using Muine.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Muine.App.ViewModels;

public partial class RadioViewModel : ViewModelBase
{
    private readonly RadioStationService _radioStationService;
    private readonly RadioMetadataService _radioMetadataService;
    private readonly RadioBrowserService? _radioBrowserService;

    private ObservableCollection<RadioStation> _stations = new();
    public ObservableCollection<RadioStation> Stations
    {
        get => _stations;
        set => SetProperty(ref _stations, value);
    }

    [ObservableProperty]
    private RadioStation? _selectedStation;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<CategoryNode> _categoryTree = new();

    [ObservableProperty]
    private CategoryNode? _selectedCategory;

    [ObservableProperty]
    private ObservableCollection<RadioStation> _onlineSearchResults = new();

    [ObservableProperty]
    private RadioStation? _selectedOnlineStation;

    [ObservableProperty]
    private string _onlineSearchQuery = string.Empty;

    [ObservableProperty]
    private bool _isOnlineSearching;

    public RadioViewModel(RadioStationService radioStationService, RadioMetadataService radioMetadataService, RadioBrowserService? radioBrowserService)
    {
        _radioStationService = radioStationService;
        _radioMetadataService = radioMetadataService;
        _radioBrowserService = radioBrowserService;
    }

    public async Task LoadStationsAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading radio stations...";

        try
        {
            var stations = await _radioStationService.GetAllStationsAsync();
            
            await BuildCategoryTreeAsync();
            
            // Initially show all stations
            Stations.Clear();
            foreach (var station in stations)
            {
                Stations.Add(station);
            }
            StatusMessage = $"Loaded {stations.Count} radio stations";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading stations: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task SearchStationsAsync()
    {
        IsLoading = true;
        StatusMessage = "Searching...";

        try
        {
            var stations = await _radioStationService.SearchStationsAsync(SearchQuery);
            Stations.Clear();
            foreach (var station in stations)
            {
                Stations.Add(station);
            }
            StatusMessage = $"Found {stations.Count} stations";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error searching: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task DeleteStationAsync(RadioStation? station)
    {
        if (station == null) return;

        try
        {
            // Store the category info before deletion to check if it becomes empty
            var categoryBeforeDeletion = station.FullCategory;
            
            await _radioStationService.DeleteStationAsync(station.Id);
            Stations.Remove(station);
            
            // Rebuild category tree to reflect the deletion
            await BuildCategoryTreeAsync();
            
            // Check if the category the station was in still exists
            var categoryStillExists = CategoryTree.Any(node => 
                node.Name == categoryBeforeDeletion || 
                node.Children.Any(child => $"{node.Name} > {child.Name}" == categoryBeforeDeletion));
            
            // If the category no longer exists (it was empty after deletion), show all stations
            if (!categoryStillExists && !string.IsNullOrEmpty(categoryBeforeDeletion))
            {
                // Load all stations to show the full list
                var allStations = await _radioStationService.GetAllStationsAsync();
                Stations.Clear();
                foreach (var s in allStations)
                {
                    Stations.Add(s);
                }
                
                // Clear the selected category to indicate we're showing all stations
                SelectedCategory = null;
                
                StatusMessage = $"Deleted '{station.Name}' - category '{categoryBeforeDeletion}' was removed (empty)";
            }
            else
            {
                StatusMessage = $"Deleted '{station.Name}'";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting station: {ex.Message}";
        }
    }

    private async Task BuildCategoryTreeAsync()
    {
        var grouped = await _radioStationService.GetStationsGroupedByCategoryAsync();
        var rootNodes = new Dictionary<string, CategoryNode>();

        foreach (var kvp in grouped)
        {
            var fullCategory = kvp.Key;
            var stations = kvp.Value;

            if (fullCategory == "Uncategorized")
            {
                // Add a single "Uncategorized" node
                var uncategorizedNode = new CategoryNode
                {
                    Name = "Uncategorized",
                    Stations = new ObservableCollection<RadioStation>(stations)
                };
                rootNodes["Uncategorized"] = uncategorizedNode;
                continue;
            }

            // Split the category by " > " to handle hierarchy
            var parts = fullCategory.Split(new[] { " > " }, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length == 1)
            {
                // Root category
                if (!rootNodes.ContainsKey(parts[0]))
                {
                    rootNodes[parts[0]] = new CategoryNode { Name = parts[0] };
                }
                
                foreach (var station in stations)
                {
                    rootNodes[parts[0]].Stations.Add(station);
                }
            }
            else if (parts.Length == 2)
            {
                // Parent > Child category
                if (!rootNodes.ContainsKey(parts[0]))
                {
                    rootNodes[parts[0]] = new CategoryNode { Name = parts[0] };
                }

                var parentNode = rootNodes[parts[0]];
                var childNode = parentNode.Children.FirstOrDefault(c => c.Name == parts[1]);
                
                if (childNode == null)
                {
                    childNode = new CategoryNode { Name = parts[1] };
                    parentNode.Children.Add(childNode);
                }

                foreach (var station in stations)
                {
                    childNode.Stations.Add(station);
                }
            }
        }

        CategoryTree = new ObservableCollection<CategoryNode>(rootNodes.Values.OrderBy(n => n.Name));
    }

    partial void OnSelectedCategoryChanged(CategoryNode? value)
    {
        if (value != null)
        {
            // Clear and repopulate the existing collection instead of replacing it
            Stations.Clear();
            foreach (var station in value.Stations)
            {
                Stations.Add(station);
            }
            StatusMessage = $"Showing {Stations.Count} stations in '{value.Name}'";
        }
    }

    /// <summary>
    /// Search for radio stations online using Radio-Browser.info
    /// Searches by name, city, and genre using a single search box
    /// </summary>
    public async Task SearchOnlineAsync()
    {
        if (string.IsNullOrWhiteSpace(OnlineSearchQuery))
        {
            StatusMessage = "Please enter a search term";
            return;
        }

        if (_radioBrowserService == null)
        {
            StatusMessage = "Online radio search is unavailable (network initialization failed)";
            return;
        }

        IsOnlineSearching = true;
        StatusMessage = $"Searching online for '{OnlineSearchQuery}'...";

        try
        {
            var results = await _radioBrowserService.SearchStationsAsync(OnlineSearchQuery, limit: 100);
            
            OnlineSearchResults.Clear();
            foreach (var station in results)
            {
                OnlineSearchResults.Add(station);
            }
            
            StatusMessage = $"Found {results.Count} stations online";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error searching online: {ex.Message}";
        }
        finally
        {
            IsOnlineSearching = false;
        }
    }

    /// <summary>
    /// Add a selected online station to the local library
    /// </summary>
    public async Task AddOnlineStationToLibraryAsync(RadioStation? station)
    {
        if (station == null)
        {
            StatusMessage = "Please select a station to add";
            return;
        }

        try
        {
            // Check if station already exists by URL
            var existing = await _radioStationService.GetStationByUrlAsync(station.Url);
            if (existing != null)
            {
                StatusMessage = $"Station '{station.Name}' already exists in your library";
                return;
            }

            // Save the station to local database
            var id = await _radioStationService.SaveStationAsync(station);
            
            // Reload local stations to show the new addition
            await LoadStationsAsync();
            
            StatusMessage = $"Added '{station.Name}' to your library";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error adding station: {ex.Message}";
        }
    }

    /// <summary>
    /// Clear online search results
    /// </summary>
    public void ClearOnlineSearch()
    {
        OnlineSearchResults.Clear();
        OnlineSearchQuery = string.Empty;
        SelectedOnlineStation = null;
        StatusMessage = string.Empty;
    }
}

public partial class CategoryNode : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private ObservableCollection<CategoryNode> _children = new();

    [ObservableProperty]
    private ObservableCollection<RadioStation> _stations = new();

    [ObservableProperty]
    private bool _isExpanded;

    public bool HasChildren => Children.Count > 0;
    public bool HasStations => Stations.Count > 0;
}
