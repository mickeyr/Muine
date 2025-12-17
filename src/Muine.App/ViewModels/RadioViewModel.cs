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

    public RadioViewModel(RadioStationService radioStationService, RadioMetadataService radioMetadataService)
    {
        _radioStationService = radioStationService;
        _radioMetadataService = radioMetadataService;
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
            Stations = new ObservableCollection<RadioStation>(stations);
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
            Stations = new ObservableCollection<RadioStation>(stations);
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
            await _radioStationService.DeleteStationAsync(station.Id);
            Stations.Remove(station);
            StatusMessage = $"Deleted '{station.Name}'";
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
            var stationsList = new ObservableCollection<RadioStation>(value.Stations);
            Stations = stationsList;
            StatusMessage = $"Showing {stationsList.Count} stations in '{value.Name}'";
        }
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
