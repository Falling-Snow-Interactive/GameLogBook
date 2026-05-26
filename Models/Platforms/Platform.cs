
using GameLogBook.Models.Libraries.Entries;

namespace GameLogBook.Models.Platforms;

public class Platform(string name) : ILibraryEntry
{
    // Database
    public int ID { get; init; }

    // Information
    public string Name { get; set; } = name;
    public string? ShortName { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public string? Summary { get; set; }
    
    // Images
    public ImageRef? Cover { get; set; }
    public ImageRef? Hero { get; set; }
    public ImageRef? Logo { get; set; }
    public ImageRef? Icon { get; set; }
    
    // Relation DBs
    // TODO - Change this to a relational DB
    public int[]? ManufacturerIds { get; set; }
    
    // APIs
    public long? IGDB { get; set; }
    
    // Interface Redirects
    public DateOnly? Date => ReleaseDate;

    #region Constructors
    
    public Platform(Platform other) : this(other.Name)
    {
        ID = other.ID;

        Name = other.Name;
        ShortName = other.ShortName;
        ReleaseDate = other.ReleaseDate;
        Summary = other.Summary;
        
        ManufacturerIds = other.ManufacturerIds;
        
        Cover = other.Cover;
        Hero = other.Hero;
        Logo = other.Logo;
        Icon = other.Icon;
        
        IGDB = other.IGDB;
    }
    
    #endregion
}
