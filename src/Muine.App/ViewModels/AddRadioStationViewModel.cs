using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Muine.Core.Models;
using Muine.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Muine.App.ViewModels;

public partial class AddRadioStationViewModel : ViewModelBase
{
    private readonly RadioStationService _radioStationService;
    private readonly RadioMetadataService _radioMetadataService;
    private RadioStation? _existingStation;

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _genre = string.Empty;

    [ObservableProperty]
    private string _location = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _website = string.Empty;

    [ObservableProperty]
    private int _bitrate;

    [ObservableProperty]
    private string _category = string.Empty;

    [ObservableProperty]
    private string _parentCategory = string.Empty;

    [ObservableProperty]
    private bool _isExtracting;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _availableCategories = new();

    [ObservableProperty]
    private ObservableCollection<string> _availableParentCategories = new();

    public bool IsEditMode => _existingStation != null;
    public string WindowTitle => IsEditMode ? "Edit Radio Station" : "Add Radio Station";

    public AddRadioStationViewModel(RadioStationService radioStationService, RadioMetadataService radioMetadataService)
    {
        _radioStationService = radioStationService;
        _radioMetadataService = radioMetadataService;
        _ = LoadCategoriesAsync();
    }

    public void LoadStation(RadioStation station)
    {
        _existingStation = station;
        Url = station.Url;
        Name = station.Name;
        Genre = station.Genre;
        Location = station.Location;
        Description = station.Description;
        Website = station.Website;
        Bitrate = station.Bitrate;
        Category = station.Category;
        ParentCategory = station.ParentCategory;
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(WindowTitle));
    }

    public async Task ExtractMetadataAsync()
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            StatusMessage = "Please enter a URL first";
            return;
        }

        IsExtracting = true;
        StatusMessage = "Extracting metadata from stream...";

        try
        {
            var station = await _radioMetadataService.ExtractMetadataAsync(Url);
            
            // Only update fields that are empty
            if (string.IsNullOrEmpty(Name))
                Name = station.Name;
            if (string.IsNullOrEmpty(Genre))
                Genre = station.Genre;
            if (string.IsNullOrEmpty(Description))
                Description = station.Description;
            if (string.IsNullOrEmpty(Website))
                Website = station.Website;
            if (Bitrate == 0)
                Bitrate = station.Bitrate;

            // Update URL in case it was a playlist that resolved to actual stream
            if (!string.IsNullOrEmpty(station.Url) && station.Url != Url)
            {
                Url = station.Url;
            }

            StatusMessage = "Metadata extracted successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error extracting metadata: {ex.Message}";
        }
        finally
        {
            IsExtracting = false;
        }
    }

    public async Task<bool> SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            StatusMessage = "URL is required";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            StatusMessage = "Name is required";
            return false;
        }

        try
        {
            var station = _existingStation ?? new RadioStation();
            station.Url = Url.Trim();
            station.Name = Name.Trim();
            station.Genre = Genre.Trim();
            station.Location = Location.Trim();
            station.Description = Description.Trim();
            station.Website = Website.Trim();
            station.Bitrate = Bitrate;
            station.Category = Category.Trim();
            station.ParentCategory = ParentCategory.Trim();

            await _radioStationService.SaveStationAsync(station);
            StatusMessage = "Station saved successfully";
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving station: {ex.Message}";
            return false;
        }
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var stations = await _radioStationService.GetAllStationsAsync();
            
            var categories = stations
                .Where(s => !string.IsNullOrEmpty(s.Category))
                .Select(s => s.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            var parentCategories = stations
                .Where(s => !string.IsNullOrEmpty(s.ParentCategory))
                .Select(s => s.ParentCategory)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            AvailableCategories = new ObservableCollection<string>(categories);
            AvailableParentCategories = new ObservableCollection<string>(parentCategories);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading categories: {ex.Message}";
        }
    }
}
