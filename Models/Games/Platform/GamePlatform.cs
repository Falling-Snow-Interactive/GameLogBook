namespace GameLogBook.Models.Games.Platform;

public class GamePlatform
{
    public int GameID { get; set; }

    public Game Game { get; set; } = null!;

    public int PlatformID { get; set; }

    public Platforms.Platform Platform { get; set; } = null!;
    
    public OwnershipType Ownership { get; set; } = OwnershipType.None;
}