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
    private bool _isExpanded = false;
    
    public int TotalSongs => Albums.Sum(a => a.Songs.Count);
    public int AlbumCount => Albums.Count;
}
