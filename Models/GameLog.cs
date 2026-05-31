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
    
    #region Time Control
    
    public void SetLogStartToNow()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        StartedAt = now;

        if (EndedAt is null || EndedAt.Value < StartedAt)
        {
            EndedAt = StartedAt;
        }
    }
    
    public void SetLogEndToNow()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        EndedAt = now;

        if (EndedAt is not null && StartedAt > EndedAt.Value)
        {
            StartedAt = EndedAt.Value;
        }
    }
    
    #endregion
    
    #region Get Strings
    
    public string GetNotesPreview()
    {
        if (string.IsNullOrWhiteSpace(Notes))
        {
            return "None";
        }

        return Notes.Length <= 80 ? Notes : $"{Notes[..80]}...";
    }
    
    #region Date/Time Strings

    #region Date
    
    public string GetStartedShortDate() => FormatShortDate(StartedAt);
    
    private static string FormatShortDate(DateTimeOffset value) => value.LocalDateTime.ToString("MMM d, yyyy");
    
    #endregion

    #region Time
    
    public string GetStartedTime()
    {
        return GetTimeString(StartedAt.LocalDateTime);
    }

    public string GetEndedTime()
    {
        return !EndedAt.HasValue ? "" : GetTimeString(EndedAt.Value.LocalDateTime);
    }

    public string GetTimeRange()
    {
        return $"{GetStartedTime()} - {GetEndedTime()}";
    }

    private static string GetTimeString(DateTime date)
    {
        TimeOnly time = TimeOnly.FromDateTime(date);
        return time.ToString("h:mm tt");
    }

    public string GetDuration()
    {
        if (EndedAt is null)
        {
            return "In progress";
        }

        TimeSpan duration = EndedAt.Value >= StartedAt ? EndedAt.Value - StartedAt : TimeSpan.Zero;

        if (duration.TotalMinutes < 1)
        {
            return "";
        }

        List<string> parts = [];

        if (duration.TotalHours > 0)
        {
            parts.Add($"{duration.TotalHours:0}h");
        }

        if (duration.Minutes > 0 || parts.Count == 0)
        {
            parts.Add($"{duration.Minutes:0}m");
        }

        return string.Join(' ', parts);
    }
    
    private string FormatTimeRange()
    {
        return EndedAt is null
                   ? $"{StartedAt.LocalDateTime:h:mm tt} - In progress"
                   : $"{StartedAt.LocalDateTime:h:mm tt} - {EndedAt.Value.LocalDateTime:h:mm tt}";
    }
    
    #endregion
    
    #endregion
    
    #endregion
}
