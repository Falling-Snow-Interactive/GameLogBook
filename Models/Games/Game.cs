namespace GameLogBook.Models.Games;

public class Game
{
    public int IgdbId { get; set; }
    public string? Title { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public string? Summary { get; set; }
    public string? CoverUrl { get; set; }
}