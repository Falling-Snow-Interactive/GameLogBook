using VGL.Models.Games;
using VGL.Models.Games.Platforms;
using Platform = VGL.Models.Platforms.Platform;

namespace VGL.Models.Users;

public class UserGamePlatformOwnership
{
    public int UserProfileID { get; set; }

    public UserProfile UserProfile { get; set; } = null!;

    public int GameID { get; set; }

    public Game Game { get; set; } = null!;

    public int PlatformID { get; set; }

    public Platform Platform { get; set; } = null!;

    public OwnershipType Ownership { get; set; } = OwnershipType.None;
}
