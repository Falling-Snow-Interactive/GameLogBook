namespace GameLogBook.Models.Platforms;

public class Platform
{
    public int ID { get; set; }

    public long IgdbId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateOnly? ReleaseDate { get; set; }

    public int[] ManufacturerIds { get; set; } = [];

    public int[] GameIds { get; set; } = [];
}
