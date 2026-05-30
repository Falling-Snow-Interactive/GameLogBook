using VGL.Models.Games;
using VGL.Models.Users;
using Platform = VGL.Models.Platforms.Platform;

namespace VGL.Models;

public class GameLog
{
    public int ID { get; set; }

    public int UserProfileID { get; set; }

    public UserProfile UserProfile { get; set; } = null!;

    public int GameID { get; set; }

    public Game Game { get; set; } = null!;

    public int PlaythroughID { get; set; }

    public Playthrough Playthrough { get; set; } = null!;

    public int PlatformID { get; set; }

    public Platform Platform { get; set; } = null!;

    public string? Title { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public string? Location { get; set; }

    public string? Notes { get; set; }

    public PlaythroughStatus? StatusChange { get; set; }

    public GameLog(GameLog copy)
    {
        ID = copy.ID;
        UserProfileID = copy.UserProfileID;
        GameID = copy.GameID;
        PlaythroughID = copy.PlaythroughID;
        PlatformID = copy.PlatformID;
        Title = copy.Title;
        StartedAt = copy.StartedAt;
        EndedAt = copy.EndedAt;
        Location = copy.Location;
        Notes = copy.Notes;
        StatusChange = copy.StatusChange;
    }
}
