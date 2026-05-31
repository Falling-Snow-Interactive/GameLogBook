using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VGL.Components.Popups;
using VGL.Models;
using VGL.Models.Companies;
using VGL.Models.Games;
using VGL.Services.NowPlaying;
using PlatformModel = VGL.Models.Platforms.Platform;

namespace VGL.Components.Pages;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class LogsPage : LogbookPageBase<GameLog>
{
    protected override DbSet<GameLog> EntitySet => DbContext.GameLogs;
    
    [Inject]
    private NowPlayingSessionService NowPlaying { get; set; } = null!;
    
    private IReadOnlyList<Playthrough> playthroughs = [];
    private IReadOnlyList<Game> games = [];
    private IReadOnlyList<PlatformModel> platforms = [];
    private IReadOnlyList<Company> companies = [];
    
    #region Sorting
    
    protected override string GetSortKey(GameLog item)
    {
        return item.StartedAt.ToString("O");
    }
    
    #endregion
    
    #region Initialize

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadPickerDataAsync();
    }
    
    #endregion

    #region Load
    
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
    
    private async Task LoadPickerDataAsync()
    {
        if (UserSession.CurrentUserID is null)
        {
            playthroughs = [];
            games = [];
            platforms = [];
            companies = [];
            return;
        }

        int userProfileId = UserSession.CurrentUserID.Value;

        playthroughs = await DbContext.Playthroughs
                                      .AsNoTracking()
                                      .Where(playthrough => playthrough.UserProfileID == userProfileId)
                                      .Include(playthrough => playthrough.Game)
                                      .Include(playthrough => playthrough.Platform)
                                      .OrderBy(playthrough => playthrough.Name)
                                      .ToListAsync();

        games = await LoadLibraryGamesAsync();
        platforms = await LoadLibraryPlatformsAsync();
        companies = await LoadCompaniesAsync();
    }
    
    #endregion
    
    #region Log Control

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

    private async Task Refresh()
    {
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
    
    #endregion
    
    #region Popups

    protected override async Task OpenAddPopup()
    {
        GameLog? log = await PopupService.ShowAsync<AddGameLogPopup, GameLog>(new Dictionary<string, object?>
                                                                              {
                                                                                  [nameof(AddGameLogPopup.Playthroughs)] = playthroughs,
                                                                                  [nameof(AddGameLogPopup.LibraryGames)] = games,
                                                                                  [nameof(AddGameLogPopup.Platforms)] = platforms,
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
                                                                                         [nameof(AddGameLogPopup.Playthroughs)] = playthroughs,
                                                                                         [nameof(AddGameLogPopup.LibraryGames)] = games,
                                                                                         [nameof(AddGameLogPopup.Platforms)] = platforms,
                                                                                         [nameof(AddGameLogPopup.OnGameAdded)] = new Func<Task<Game?>>(AddGameFromPicker),
                                                                                         [nameof(AddGameLogPopup.OnPlatformAdded)] = new Func<Task<PlatformModel?>>(AddPlatformFromPicker)
                                                                                     });

        if (updatedLog is not null)
        {
            await UpdateLog(updatedLog);
        }
    }
    
    #endregion
    
    #region Pickers

    private async Task<Game?> AddGameFromPicker()
    {
        Game? game = await OpenAddGameToLibraryPopupAsync(companies, platforms);
        await LoadPickerDataAsync();
        return game;
    }

    private async Task<PlatformModel?> AddPlatformFromPicker()
    {
        PlatformModel? platform = await OpenAddPlatformToLibraryPopupAsync(games, companies);
        await LoadPickerDataAsync();
        return platform;
    }
    
    #endregion
}
