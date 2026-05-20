namespace GameLogBook.Models.Platforms;

public class Platform
{
    private int[] manufacturerIds = [];
    private int[] gameIds = [];

    public int ID { get; set; }

    public long IgdbId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? CoverUrl { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public int[] ManufacturerIds
    {
        get => manufacturerIds;
        set => manufacturerIds = value ?? [];
    }

    public int[] GameIds
    {
        get => gameIds;
        set => gameIds = value ?? [];
    }
}
