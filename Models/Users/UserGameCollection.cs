using VGL.Models.Games;

namespace VGL.Models.Users;

public class UserGameCollection
{
    public int UserProfileID { get; set; }

    public UserProfile UserProfile { get; set; } = null!;

    public int GameID { get; set; }

    public Game Game { get; set; } = null!;

    public int Rating { get; set; }

    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;
}
