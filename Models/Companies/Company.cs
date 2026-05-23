namespace GameLogBook.Models.Companies;

public class Company
{
    public int Id { get; set; }

    public long? IgdbId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ImagePath { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? PendingImageUrl { get; set; }

    public DateTimeOffset? LastSyncedAt { get; set; }
}
