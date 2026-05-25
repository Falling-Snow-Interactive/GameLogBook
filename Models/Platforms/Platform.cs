
using GameLogBook.Models.Libraries.Entries;

namespace GameLogBook.Models.Platforms;

public class Platform(string name) : ILibraryEntry
{
    // Database
    public int ID { get; set; }

    // Information
    public string Name { get; set; } = name;
    public string? Abbreviation { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public string? Summary { get; set; }
    
    // TODO - Change this to a relational DB
    public int[]? ManufacturerIds { get; set; }
    
    // Images
    public ImageRef? Cover { get; set; }
    public ImageRef? Hero { get; set; }
    public ImageRef? Logo { get; set; }
    
    // Relation DBs
    
    // IGDB
    public long? IgdbId { get; set; }

    public Platform(Platform other) : this(other.Name)
    {
        ID = other.ID;

        Name = other.Name;
        Abbreviation = other.Abbreviation;
        ReleaseDate = other.ReleaseDate;
        Summary = other.Summary;
        
        ManufacturerIds = other.ManufacturerIds;
        
        Cover = other.Cover;
        Hero = other.Hero;
        Logo = other.Logo;
        
        IgdbId = other.IgdbId;
    }
}
