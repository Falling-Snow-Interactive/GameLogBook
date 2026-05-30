using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VGL.Components.Popups;
using VGL.Models;
using VGL.Models.Companies;
using VGL.Models.Games;
using VGL.Services.NowPlaying;
using PlatformModel = VGL.Models.Platforms.Platform;

namespace VGL.Components.Pages;

public partial class LogsPage : LogbookPageBase<GameLog>
{
    public IReadOnlyList<Playthrough> Playthroughs { get; set; } = [];
    public IReadOnlyList<Game> Games { get; set; } = [];
    public IReadOnlyList<PlatformModel> Platforms { get; set; } = [];
    private IReadOnlyList<Company> companies = [];

    [Inject]
    private NowPlayingSessionService NowPlaying { get; set; } = null!;

    protected override DbSet<GameLog> EntitySet => DbContext.GameLogs;

    protected override string GetSortKey(GameLog item)
    {
        return item.StartedAt.ToString("O");
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadPickerDataAsync();
    }

    protected override async Task LoadItemsAsync()
    {
        if (UserSession.CurrentUserID is null)
        {
            Items = [];
            return;
        }

        List<GameLog> logs = await DbContext.GameLogs
                                            .AsNoTracking()
                                            .Where(log => log.UserProfileID == UserSession.CurrentUserID.Value)
                                            .Include(log => log.Game)
                                            .Include(log => log.Playthrough)
                                            .Include(log => log.Platform)
                                            .ToListAsync();

        Items = logs
                .OrderByDescending(log => log.StartedAt)
                .ToList();
    }

    private async Task AddLog(GameLog log)
    {
        if (UserSession.CurrentUserID is null)
        {
            return;
        }

        log.UserProfileID = UserSession.CurrentUserID.Value;
        DbContext.GameLogs.Add(log);
        await DbContext.SaveChangesAsync();
        await NowPlaying.RecalculatePlaythroughStatusAsync(log.PlaythroughID);
        await LoadItemsAsync();
        await LoadPickerDataAsync();
    }

    private async Task UpdateLog(GameLog updatedLog)
    {
        GameLog? existingLog = await DbContext.GameLogs
                                             .FirstOrDefaultAsync(log => log.ID == updatedLog.ID
                                                                         && log.UserProfileID == UserSession.CurrentUserID);

        if (existingLog is null)
        {
            return;
        }

        int originalPlaythroughId = existingLog.PlaythroughID;

        existingLog.PlaythroughID = updatedLog.PlaythroughID;
        existingLog.GameID = updatedLog.GameID;
        existingLog.PlatformID = updatedLog.PlatformID;
        existingLog.Title = updatedLog.Title;
        existingLog.StartedAt = updatedLog.StartedAt;
        existingLog.EndedAt = updatedLog.EndedAt;
        existingLog.Location = updatedLog.Location;
        existingLog.Notes = updatedLog.Notes;
        existingLog.StatusChange = updatedLog.StatusChange;

        await DbContext.SaveChangesAsync();
        await NowPlaying.RecalculatePlaythroughStatusAsync(originalPlaythroughId);

        if (originalPlaythroughId != updatedLog.PlaythroughID)
        {
            await NowPlaying.RecalculatePlaythroughStatusAsync(updatedLog.PlaythroughID);
        }

        await LoadItemsAsync();
        await LoadPickerDataAsync();
    }

    private async Task RemoveLog(GameLog log)
    {
        GameLog? existingLog = await DbContext.GameLogs
                                             .FirstOrDefaultAsync(item => item.ID == log.ID
                                                                         && item.UserProfileID == UserSession.CurrentUserID);

        if (existingLog is null)
        {
            return;
        }

        int playthroughId = existingLog.PlaythroughID;
        DbContext.GameLogs.Remove(existingLog);
        await DbContext.SaveChangesAsync();
        await NowPlaying.RecalculatePlaythroughStatusAsync(playthroughId);
        await LoadItemsAsync();
        await LoadPickerDataAsync();
    }

    private async Task SetLogStartToNow(GameLog log)
    {
        GameLog? existingLog = await DbContext.GameLogs
                                             .FirstOrDefaultAsync(item => item.ID == log.ID
                                                                         && item.UserProfileID == UserSession.CurrentUserID);

        if (existingLog is null)
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.Now;
        existingLog.StartedAt = now;

        if (existingLog.EndedAt is null || existingLog.EndedAt.Value < existingLog.StartedAt)
        {
            existingLog.EndedAt = existingLog.StartedAt;
        }

        await DbContext.SaveChangesAsync();
        await LoadItemsAsync();
    }

    private async Task SetLogEndToNow(GameLog log)
    {
        GameLog? existingLog = await DbContext.GameLogs
                                             .FirstOrDefaultAsync(item => item.ID == log.ID
                                                                         && item.UserProfileID == UserSession.CurrentUserID);

        if (existingLog is null)
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.Now;
        existingLog.EndedAt = now;

        if (existingLog.EndedAt is not null && existingLog.StartedAt > existingLog.EndedAt.Value)
        {
            existingLog.StartedAt = existingLog.EndedAt.Value;
        }

        await DbContext.SaveChangesAsync();
        await LoadItemsAsync();
    }

    protected override async Task OpenAddPopup()
    {
        GameLog? log = await PopupService.ShowAsync<AddGameLogPopup, GameLog>(
            new Dictionary<string, object?>
            {
                [nameof(AddGameLogPopup.Playthroughs)] = Playthroughs,
                [nameof(AddGameLogPopup.LibraryGames)] = Games,
                [nameof(AddGameLogPopup.Platforms)] = Platforms,
                [nameof(AddGameLogPopup.OnGameAdded)] = new Func<Task<Game?>>(AddGameFromPicker),
                [nameof(AddGameLogPopup.OnPlatformAdded)] = new Func<Task<PlatformModel?>>(AddPlatformFromPicker)
            });

        if (log is not null)
        {
            await AddLog(log);
        }
    }

    private async Task OpenEditPopup(GameLog log)
    {
        GameLog editableLog = new()
                              {
                                  ID = log.ID,
                                  UserProfileID = log.UserProfileID,
                                  GameID = log.GameID,
                                  PlaythroughID = log.PlaythroughID,
                                  PlatformID = log.PlatformID,
                                  Title = log.Title,
                                  StartedAt = log.StartedAt,
                                  EndedAt = log.EndedAt,
                                  Location = log.Location,
                                  Notes = log.Notes,
                                  StatusChange = log.StatusChange
                              };

        GameLog? updatedLog = await PopupService.ShowAsync<AddGameLogPopup, GameLog>(
            new Dictionary<string, object?>
            {
                [nameof(AddGameLogPopup.InitialLog)] = editableLog,
                [nameof(AddGameLogPopup.Playthroughs)] = Playthroughs,
                [nameof(AddGameLogPopup.LibraryGames)] = Games,
                [nameof(AddGameLogPopup.Platforms)] = Platforms,
                [nameof(AddGameLogPopup.OnGameAdded)] = new Func<Task<Game?>>(AddGameFromPicker),
                [nameof(AddGameLogPopup.OnPlatformAdded)] = new Func<Task<PlatformModel?>>(AddPlatformFromPicker)
            });

        if (updatedLog is not null)
        {
            await UpdateLog(updatedLog);
        }
    }

    private async Task LoadPickerDataAsync()
    {
        if (UserSession.CurrentUserID is null)
        {
            Playthroughs = [];
            Games = [];
            Platforms = [];
            companies = [];
            return;
        }

        int userProfileId = UserSession.CurrentUserID.Value;

        Playthroughs = await DbContext.Playthroughs
                                      .AsNoTracking()
                                      .Where(playthrough => playthrough.UserProfileID == userProfileId)
                                      .Include(playthrough => playthrough.Game)
                                      .Include(playthrough => playthrough.Platform)
                                      .OrderBy(playthrough => playthrough.Name)
                                      .ToListAsync();

        Games = await LoadLibraryGamesAsync();
        Platforms = await LoadLibraryPlatformsAsync();
        companies = await LoadCompaniesAsync();
    }

    private async Task<Game?> AddGameFromPicker()
    {
        Game? game = await OpenAddGameToLibraryPopupAsync(companies, Platforms);
        await LoadPickerDataAsync();
        return game;
    }

    private async Task<PlatformModel?> AddPlatformFromPicker()
    {
        PlatformModel? platform = await OpenAddPlatformToLibraryPopupAsync(Games, companies);
        await LoadPickerDataAsync();
        return platform;
    }

    private static string GetLogTitle(GameLog log)
    {
        return string.IsNullOrWhiteSpace(log.Title)
                   ? $"{log.Game.Name} session"
                   : log.Title;
    }

    private static string GetLogSummary(GameLog log)
    {
        string status = log.StatusChange is null ? "No status change" : $"Changed to {log.StatusChange}";
        return $"{log.Playthrough.Name} · {FormatDuration(log)} · {status}";
    }

    private static string GetNotesPreview(GameLog log)
    {
        if (string.IsNullOrWhiteSpace(log.Notes))
        {
            return "None";
        }

        return log.Notes.Length <= 80 ? log.Notes : $"{log.Notes[..80]}...";
    }

    private static string FormatDateTime(DateTimeOffset value)
    {
        return value.LocalDateTime.ToString("MMM d, yyyy h:mm tt");
    }

    private static string FormatDateTime(DateTimeOffset? value)
    {
        return value is null ? "In progress" : FormatDateTime(value.Value);
    }

    private static string FormatShortDate(DateTimeOffset value)
    {
        return value.LocalDateTime.ToString("MMM d, yyyy");
    }

    private static string FormatTimeRange(GameLog log)
    {
        return log.EndedAt is null
                   ? $"{log.StartedAt.LocalDateTime:h:mm tt} - In progress"
                   : $"{log.StartedAt.LocalDateTime:h:mm tt} - {log.EndedAt.Value.LocalDateTime:h:mm tt}";
    }

    private static string FormatDuration(GameLog log)
    {
        if (log.EndedAt is null)
        {
            return "In progress";
        }

        TimeSpan duration = log.EndedAt.Value >= log.StartedAt ? log.EndedAt.Value - log.StartedAt : TimeSpan.Zero;

        if (duration.TotalMinutes < 1)
        {
            return "0 Minutes";
        }

        List<string> parts = [];

        // if (duration.Days > 0)
        // {
        //     parts.Add($"{duration.Days} {(duration.Days == 1 ? "Day" : "Days")}");
        // }

        int h = duration.Hours + duration.Days * 24;
        if (h > 0)
        {
            // parts.Add($"{h} {(h == 1 ? "Hour" : "Hours")}");
            parts.Add($"{h}h");
        }

        if (duration.Minutes > 0 || parts.Count == 0)
        {
            // parts.Add($"{duration.Minutes} {(duration.Minutes == 1 ? "Minute" : "Minutes")}");
            parts.Add($"{duration.Minutes}m");
        }

        return string.Join(' ', parts);
    }
}
