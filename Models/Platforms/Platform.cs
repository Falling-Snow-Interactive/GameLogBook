namespace GameLogBook.Models.Platforms;

public class Platform
{
    public int ID { get; set; }

    public long? IgdbId { get; set; }

    public string Name { get; set; }
    public string Abbreviation { get; set; }

    public string? ImagePath { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? PendingImageUrl { get; set; }

    public DateOnly? ReleaseDate { get; set; }
    
    public int[]? ManufacturerIds { get; set; }

    public int[]? GameIds { get; set; }

    public Platform(long? igdbId, string name, string abbreviation, string? imagePath, DateOnly? releaseDate, 
                    int[] manufacturerIds, int[] gameIds)
    {
        IgdbId = igdbId;
        Abbreviation = abbreviation;
        ImagePath = imagePath;
        ReleaseDate = releaseDate;
        ManufacturerIds = manufacturerIds;
        GameIds = gameIds;
        Name = name;
    }

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
        GameIds = other.GameIds;
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
        GameIds = other.GameIds;
    }
}
