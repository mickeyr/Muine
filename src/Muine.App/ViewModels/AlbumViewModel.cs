using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Muine.Core.Models;

namespace Muine.App.ViewModels;

public partial class AlbumViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private string _artist = string.Empty;
    
    [ObservableProperty]
    private string _year = string.Empty;
    
    [ObservableProperty]
    private string? _coverImagePath;
    
    [ObservableProperty]
    private ObservableCollection<Song> _songs = new();
    
    [ObservableProperty]
    private bool _isExpanded = false;
    
    public int TrackCount => Songs.Count;
    public string DisplayName => !string.IsNullOrEmpty(Year) ? $"{Name} ({Year})" : Name;
}
