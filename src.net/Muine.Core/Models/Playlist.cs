namespace Muine.Core.Models;

public class Playlist
{
    public List<Song> Songs { get; set; } = new();
    public int CurrentIndex { get; set; } = -1;
    public Song? CurrentSong => CurrentIndex >= 0 && CurrentIndex < Songs.Count ? Songs[CurrentIndex] : null;
    public bool HasNext => CurrentIndex < Songs.Count - 1;
    public bool HasPrevious => CurrentIndex > 0;
    public int Count => Songs.Count;
    
    public void Add(Song song)
    {
        Songs.Add(song);
    }
    
    public void AddRange(IEnumerable<Song> songs)
    {
        Songs.AddRange(songs);
    }
    
    public void Insert(int index, Song song)
    {
        Songs.Insert(index, song);
    }
    
    public void Remove(Song song)
    {
        var index = Songs.IndexOf(song);
        if (index >= 0)
        {
            Songs.RemoveAt(index);
            if (index < CurrentIndex)
            {
                CurrentIndex--;
            }
            else if (CurrentIndex >= Songs.Count)
            {
                CurrentIndex = Songs.Count - 1;
            }
        }
    }
    
    public void RemoveAt(int index)
    {
        if (index >= 0 && index < Songs.Count)
        {
            Songs.RemoveAt(index);
            if (index < CurrentIndex)
            {
                CurrentIndex--;
            }
            else if (CurrentIndex >= Songs.Count)
            {
                CurrentIndex = Songs.Count - 1;
            }
        }
    }
    
    public void Clear()
    {
        Songs.Clear();
        CurrentIndex = -1;
    }
    
    public Song? Next()
    {
        if (HasNext)
        {
            CurrentIndex++;
            return CurrentSong;
        }
        return null;
    }
    
    public Song? Previous()
    {
        if (HasPrevious)
        {
            CurrentIndex--;
            return CurrentSong;
        }
        return null;
    }
    
    public void MoveTo(int index)
    {
        if (index >= 0 && index < Songs.Count)
        {
            CurrentIndex = index;
        }
    }
    
    public void Move(int oldIndex, int newIndex)
    {
        if (oldIndex >= 0 && oldIndex < Songs.Count && newIndex >= 0 && newIndex < Songs.Count)
        {
            var song = Songs[oldIndex];
            Songs.RemoveAt(oldIndex);
            Songs.Insert(newIndex, song);
            
            // Update current index if needed
            if (CurrentIndex == oldIndex)
            {
                CurrentIndex = newIndex;
            }
            else if (oldIndex < CurrentIndex && newIndex >= CurrentIndex)
            {
                CurrentIndex--;
            }
            else if (oldIndex > CurrentIndex && newIndex <= CurrentIndex)
            {
                CurrentIndex++;
            }
        }
    }
}
