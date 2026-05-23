namespace GameLogBook.Models.Games;

public class Game
{
    public int Id { get; set; }

    public long IgdbId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Summary { get; set; }
    
    public GameType GameType { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public Cover? Cover { get; set; }

    public int[] DeveloperCompanyIds { get; set; } = [];

    public int[] PublisherCompanyIds { get; set; } = [];
    
    public int Rating { get; set; }
    
    public Ownership Ownership { get; set; }
}
