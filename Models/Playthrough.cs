using VGL.Models.Users;

namespace VGL.Models;

public class Playthrough
{
    public int ID { get; set; }

    public int UserProfileID { get; set; }

    public UserProfile UserProfile { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public int[] GameIds { get; set; } = [];
}
