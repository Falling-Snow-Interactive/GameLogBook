using GameLogBook.Models.Libraries.Entries;

namespace GameLogBook.Models.Companies;

public class Company : ILibraryEntry
{
    public int ID { get; set; }

    // Information
    public string Name { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public DateOnly? FoundedDate { get; set; }
    
    // Images
    public ImageRef? Cover { get; set; }
    public ImageRef? Hero { get; set; }
    public ImageRef? Logo { get; set; }
    public ImageRef? Icon { get; set; }

    public string? ImagePath { get; set; }
    
    // Online API IDs
    public long? IGDB { get; set; }
    
    // Entry
    public DateOnly? Date => FoundedDate;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? PendingImageUrl { get; set; }

    public DateTimeOffset? LastSyncedAt { get; set; }
}
