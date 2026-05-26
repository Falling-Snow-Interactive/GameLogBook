using Platform = VGL.Models.Platforms.Platform;

namespace VGL.Models.Games.Platforms;

public class GamePlatformRelation
{
    public int GameID { get; set; }

    public Game Game { get; set; } = null!;

    public int PlatformID { get; set; }

    public Platform Platform { get; set; } = null!;
    
    public OwnershipType Ownership { get; set; } = OwnershipType.None;
}