using System.ComponentModel.DataAnnotations.Schema;
using VGL.Models.Games;
using VGL.Models.Users;
using Platform = VGL.Models.Platforms.Platform;

namespace VGL.Models;

public class Playthrough
{
    public int ID { get; set; }

    public int UserProfileID { get; set; }

    public UserProfile UserProfile { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public PlaythroughStatus Status { get; set; } = PlaythroughStatus.NotStarted;

    public int? GameID { get; set; }

    public Game? Game { get; set; }

    public int? PlatformID { get; set; }

    public Platform? Platform { get; set; }

    public int? PlaythroughRunID { get; set; }

    public PlaythroughRun? PlaythroughRun { get; set; }

    public DateTimeOffset? ManualStartedAt { get; set; }

    public DateTimeOffset? ManualFinishedAt { get; set; }

    public DateTimeOffset? ManualMasteredAt { get; set; }

    public List<GameLog> Logs { get; set; } = [];

    [NotMapped]
    public TimeSpan TotalPlaytime => Logs
                                     .Where(log => log.EndedAt is not null && log.EndedAt.Value >= log.StartedAt)
                                     .Aggregate(TimeSpan.Zero, (total, log) => total + (log.EndedAt!.Value - log.StartedAt));

    [NotMapped]
    public DateTimeOffset? DerivedStartedAt => Logs.Count == 0 ? null : Logs.Min(log => log.StartedAt);

    [NotMapped]
    public DateTimeOffset? DerivedFinishedAt => Logs
                                                .Where(log => log.StatusChange is PlaythroughStatus.Finished or PlaythroughStatus.Mastered)
                                                .OrderBy(log => log.StartedAt)
                                                .Select(log => (DateTimeOffset?)log.StartedAt)
                                                .FirstOrDefault();

    [NotMapped]
    public DateTimeOffset? DerivedMasteredAt => Logs
                                                .Where(log => log.StatusChange == PlaythroughStatus.Mastered)
                                                .OrderBy(log => log.StartedAt)
                                                .Select(log => (DateTimeOffset?)log.StartedAt)
                                                .FirstOrDefault();

    [NotMapped]
    public DateTimeOffset? StartedAt => ManualStartedAt ?? DerivedStartedAt;

    [NotMapped]
    public DateTimeOffset? FinishedAt => ManualFinishedAt ?? DerivedFinishedAt;

    [NotMapped]
    public DateTimeOffset? MasteredAt => ManualMasteredAt ?? DerivedMasteredAt;

    [NotMapped]
    public DateTimeOffset? LastPlayedAt => Logs
                                           .Where(log => log.EndedAt is not null)
                                           .Select(log => log.EndedAt)
                                           .DefaultIfEmpty()
                                           .Max();

    [NotMapped]
    public TimeSpan? AverageSessionLength
    {
        get
        {
            int completedLogCount = Logs.Count(log => log.EndedAt is not null);
            return completedLogCount == 0 ? null : TotalPlaytime / completedLogCount;
        }
    }

    [NotMapped]
    public TimeSpan? LongestSession => Logs
                                       .Where(log => log.EndedAt is not null && log.EndedAt.Value >= log.StartedAt)
                                       .Select(log => log.EndedAt!.Value - log.StartedAt)
                                       .DefaultIfEmpty()
                                       .Max();
}
