namespace GameLogBook.Models.Platforms;

public class Platform
{
    // Database
    public int ID { get; set; }

    // Information
    public string Name { get; set; }
    public string Abbreviation { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public int[]? ManufacturerIds { get; set; }
    
    // Images
    
    // IGDB
    public long? IgdbId { get; set; }
    
    // Obsolete
    public string? ImagePath { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? PendingImageUrl { get; set; }

    public Platform(string name)
    {
        Name = name;
    }
    
    // public Platform(long? igdbId, string name, string abbreviation, string? imagePath, DateOnly? releaseDate, 
    //                 int[] manufacturerIds)
    // {
    //     IgdbId = igdbId;
    //     Abbreviation = abbreviation;
    //     ImagePath = imagePath;
    //     ReleaseDate = releaseDate;
    //     ManufacturerIds = manufacturerIds;
    //     Name = name;
    // }

    public Platform(Platform other)
    {
        ID = other.ID;
        IgdbId = other.IgdbId;
        Name = other.Name;
        Abbreviation = other.Abbreviation;
        ImagePath = other.ImagePath;
        PendingImageUrl = other.PendingImageUrl;
        ReleaseDate = other.ReleaseDate;
        ManufacturerIds = other.ManufacturerIds;
    }

    public void CopyInto(Platform other)
    {
        ID = other.ID;
        IgdbId = other.IgdbId;
        Name = other.Name;
        Abbreviation = other.Abbreviation;
        ImagePath = other.ImagePath;
        PendingImageUrl = other.PendingImageUrl;
        ReleaseDate = other.ReleaseDate;
        ManufacturerIds = other.ManufacturerIds;
    }
}
