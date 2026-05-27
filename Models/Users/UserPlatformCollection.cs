using Platform = VGL.Models.Platforms.Platform;

namespace VGL.Models.Users;

public class UserPlatformCollection
{
    public int UserProfileID { get; set; }

    public UserProfile UserProfile { get; set; } = null!;

    public int PlatformID { get; set; }

    public Platform Platform { get; set; } = null!;

    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;
}
