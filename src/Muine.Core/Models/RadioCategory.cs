namespace Muine.Core.Models;

public class RadioCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ParentCategory { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    
    // Computed properties
    public string FullPath
    {
        get
        {
            if (string.IsNullOrEmpty(ParentCategory))
                return Name;
            return $"{ParentCategory} > {Name}";
        }
    }
    
    public bool IsRootCategory => string.IsNullOrEmpty(ParentCategory);
}
