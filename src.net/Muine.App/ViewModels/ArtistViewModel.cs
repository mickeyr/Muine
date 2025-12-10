using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Muine.App.ViewModels;

public partial class ArtistViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private ObservableCollection<AlbumViewModel> _albums = new();
    
    [ObservableProperty]
    private string? _coverImagePath;
    
    public int TotalSongs => Albums.Sum(a => a.Songs.Count);
    public int AlbumCount => Albums.Count;
    
    /// <summary>
    /// Gets the cover image path from the first album with cover art, or null if none found
    /// </summary>
    public void UpdateCoverArt()
    {
        CoverImagePath = Albums.FirstOrDefault(a => !string.IsNullOrEmpty(a.CoverImagePath))?.CoverImagePath;
    }
}
