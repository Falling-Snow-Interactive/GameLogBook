namespace GameLogBook.Models.Companies;

public class Company
{
    public int Id { get; set; }
    public long? IgdbId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int[] GameIds { get; set; } = [];

    public bool IsPublisher { get; set; }

    public bool IsDeveloper { get; set; }

    public string? CoverUrl { get; set; }
}
