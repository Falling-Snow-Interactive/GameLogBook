using VGL.Models;

namespace VGL.Models.Users;

public class UserProfile
{
    public int ID { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public ImageRef? ProfilePicture { get; set; }

    public UserRole Role { get; set; } = UserRole.User;

    public bool IsHidden { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<UserGameCollection> Games { get; set; } = [];

    public List<UserPlatformCollection> Platforms { get; set; } = [];

    public List<UserGamePlatformOwnership> GamePlatformOwnerships { get; set; } = [];
}
