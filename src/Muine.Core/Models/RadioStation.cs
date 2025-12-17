namespace Muine.Core.Models;

public class RadioStation
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public int Bitrate { get; set; }
    public string Category { get; set; } = string.Empty;
    public string ParentCategory { get; set; } = string.Empty;
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    public DateTime? LastPlayed { get; set; }
    
    // Computed properties
    public string DisplayName => !string.IsNullOrEmpty(Name) ? Name : Url;
    public string FullCategory
    {
        get
        {
            if (string.IsNullOrEmpty(ParentCategory))
                return Category;
            return string.IsNullOrEmpty(Category) 
                ? ParentCategory 
                : $"{ParentCategory} > {Category}";
        }
    }
    public bool HasCategory => !string.IsNullOrEmpty(Category) || !string.IsNullOrEmpty(ParentCategory);
}
