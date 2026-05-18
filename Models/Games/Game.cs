namespace GameLogBook.Models.Games;

public class Game
{
    public int Id { get; set; }

    public long IgdbId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Summary { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public string? Developer { get; set; }

    public string? Publisher { get; set; }

    public Cover? Cover { get; set; }
}