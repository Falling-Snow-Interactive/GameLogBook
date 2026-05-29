using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VGL.Components.Popups;
using VGL.Models;
using VGL.Models.Companies;
using VGL.Models.Games;
using PlatformModel = VGL.Models.Platforms.Platform;

namespace VGL.Components.Pages;

public partial class PlaythroughsPage : LogbookPageBase<Playthrough>
{
    public IReadOnlyList<Game> Games { get; set; } = [];
    public IReadOnlyList<PlatformModel> Platforms { get; set; } = [];
    public IReadOnlyList<PlaythroughRun> PlaythroughRuns { get; set; } = [];
    private IReadOnlyList<Company> companies = [];

    protected override DbSet<Playthrough> EntitySet => DbContext.Playthroughs;

    protected override string GetSortKey(Playthrough item)
    {
        return item.Name;
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

        Items = await DbContext.Playthroughs
                               .AsNoTracking()
                               .Where(playthrough => playthrough.UserProfileID == UserSession.CurrentUserID.Value)
                               .Include(playthrough => playthrough.Game)
                               .Include(playthrough => playthrough.Platform)
                               .Include(playthrough => playthrough.PlaythroughRun)
                               .Include(playthrough => playthrough.Logs)
                               .OrderBy(playthrough => playthrough.Name)
                               .ToListAsync();
    }

    private async Task AddPlaythrough(Playthrough playthrough)
    {
        if (UserSession.CurrentUserID is null)
        {
            return;
        }

        playthrough.UserProfileID = UserSession.CurrentUserID.Value;
        await ApplyRunGroupAsync(playthrough);
        await AddItemAsync(playthrough);
        await LoadPickerDataAsync();
        await LoadItemsAsync();
    }

    private async Task UpdatePlaythrough(Playthrough updatedPlaythrough)
    {
        Playthrough? existingPlaythrough = await DbContext.Playthroughs
                                                          .FirstOrDefaultAsync(playthrough => playthrough.ID == updatedPlaythrough.ID
                                                                                              && playthrough.UserProfileID == UserSession.CurrentUserID);

        if (existingPlaythrough is null)
        {
            return;
        }

        await ApplyRunGroupAsync(updatedPlaythrough);

        existingPlaythrough.Name = updatedPlaythrough.Name.Trim();
        existingPlaythrough.Status = updatedPlaythrough.Status;
        existingPlaythrough.GameID = updatedPlaythrough.GameID;
        existingPlaythrough.PlatformID = updatedPlaythrough.PlatformID;
        existingPlaythrough.PlaythroughRunID = updatedPlaythrough.PlaythroughRunID;
        existingPlaythrough.ManualStartedAt = updatedPlaythrough.ManualStartedAt;
        existingPlaythrough.ManualFinishedAt = updatedPlaythrough.ManualFinishedAt;
        existingPlaythrough.ManualMasteredAt = updatedPlaythrough.ManualMasteredAt;

        await UpdateItemAsync();
        await LoadPickerDataAsync();
    }

    private async Task RemovePlaythrough(Playthrough playthrough)
    {
        Playthrough? existingPlaythrough = await DbContext.Playthroughs
                                                          .FirstOrDefaultAsync(item => item.ID == playthrough.ID
                                                                                       && item.UserProfileID == UserSession.CurrentUserID);

        if (existingPlaythrough is null)
        {
            return;
        }

        await RemoveItemAsync(existingPlaythrough);
        await LoadItemsAsync();
    }

    protected override async Task OpenAddPopup()
    {
        Playthrough? playthrough = await PopupService.ShowAsync<AddPlaythroughPopup, Playthrough>(
            new Dictionary<string, object?>
            {
                [nameof(AddPlaythroughPopup.LibraryGames)] = Games,
                [nameof(AddPlaythroughPopup.Platforms)] = Platforms,
                [nameof(AddPlaythroughPopup.PlaythroughRuns)] = PlaythroughRuns,
                [nameof(AddPlaythroughPopup.OnGameAdded)] = new Func<Task<Game?>>(AddGameFromPicker),
                [nameof(AddPlaythroughPopup.OnPlatformAdded)] = new Func<Task<PlatformModel?>>(AddPlatformFromPicker)
            });

        if (playthrough is not null)
        {
            await AddPlaythrough(playthrough);
        }
    }

    private async Task OpenEditPopup(Playthrough playthrough)
    {
        Playthrough editablePlaythrough = new()
                                          {
                                              ID = playthrough.ID,
                                              Name = playthrough.Name,
                                              Status = playthrough.Status,
                                              GameID = playthrough.GameID,
                                              PlatformID = playthrough.PlatformID,
                                              PlaythroughRunID = playthrough.PlaythroughRunID,
                                              ManualStartedAt = playthrough.ManualStartedAt,
                                              ManualFinishedAt = playthrough.ManualFinishedAt,
                                              ManualMasteredAt = playthrough.ManualMasteredAt
                                          };

        Playthrough? updatedPlaythrough = await PopupService.ShowAsync<AddPlaythroughPopup, Playthrough>(
            new Dictionary<string, object?>
            {
                [nameof(AddPlaythroughPopup.InitialPlaythrough)] = editablePlaythrough,
                [nameof(AddPlaythroughPopup.LibraryGames)] = Games,
                [nameof(AddPlaythroughPopup.Platforms)] = Platforms,
                [nameof(AddPlaythroughPopup.PlaythroughRuns)] = PlaythroughRuns,
                [nameof(AddPlaythroughPopup.OnGameAdded)] = new Func<Task<Game?>>(AddGameFromPicker),
                [nameof(AddPlaythroughPopup.OnPlatformAdded)] = new Func<Task<PlatformModel?>>(AddPlatformFromPicker)
            });

        if (updatedPlaythrough is not null)
        {
            await UpdatePlaythrough(updatedPlaythrough);
        }
    }

    private async Task ApplyRunGroupAsync(Playthrough playthrough)
    {
        if (UserSession.CurrentUserID is null || string.IsNullOrWhiteSpace(playthrough.PlaythroughRun?.Name))
        {
            playthrough.PlaythroughRun = null;
            return;
        }

        string runName = playthrough.PlaythroughRun.Name.Trim();
        PlaythroughRun? existingRun = await DbContext.PlaythroughRuns
                                                     .FirstOrDefaultAsync(run => run.UserProfileID == UserSession.CurrentUserID.Value
                                                                                 && run.Name == runName);

        if (existingRun is null)
        {
            existingRun = new PlaythroughRun
                          {
                              UserProfileID = UserSession.CurrentUserID.Value,
                              Name = runName
                          };

            DbContext.PlaythroughRuns.Add(existingRun);
            await DbContext.SaveChangesAsync();
        }

        playthrough.PlaythroughRunID = existingRun.ID;
        playthrough.PlaythroughRun = null;
    }

    private async Task LoadPickerDataAsync()
    {
        if (UserSession.CurrentUserID is null)
        {
            Games = [];
            Platforms = [];
            PlaythroughRuns = [];
            companies = [];
            return;
        }

        int userProfileId = UserSession.CurrentUserID.Value;

        Games = await LoadLibraryGamesAsync();
        Platforms = await LoadLibraryPlatformsAsync();
        companies = await LoadCompaniesAsync();

        PlaythroughRuns = await DbContext.PlaythroughRuns
                                         .AsNoTracking()
                                         .Where(run => run.UserProfileID == userProfileId)
                                         .OrderBy(run => run.Name)
                                         .ToListAsync();
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

    private static string GetPlaythroughSummary(Playthrough playthrough)
    {
        string game = playthrough.Game?.Name ?? "No game";
        string platform = playthrough.Platform?.Name ?? "No platform";
        return $"{game} on {platform} · {FormatPlaytime(playthrough.TotalPlaytime)} · {FormatLogCount(playthrough.Logs.Count)}";
    }

    private static string FormatLogCount(int count)
    {
        return count == 1 ? "1 log" : $"{count} logs";
    }

    private static string FormatDateTime(DateTimeOffset? value)
    {
        return value?.LocalDateTime.ToString("MMM d, yyyy h:mm tt") ?? "Not set";
    }

    private static string FormatPlaytime(TimeSpan playtime)
    {
        if (playtime <= TimeSpan.Zero)
        {
            return "0 Minutes";
        }

        List<string> parts = [];

        if (playtime.Days > 0)
        {
            parts.Add($"{playtime.Days} {(playtime.Days == 1 ? "Day" : "Days")}");
        }

        if (playtime.Hours > 0)
        {
            parts.Add($"{playtime.Hours} {(playtime.Hours == 1 ? "Hour" : "Hours")}");
        }

        if (playtime.Minutes > 0 || parts.Count == 0)
        {
            parts.Add($"{playtime.Minutes} {(playtime.Minutes == 1 ? "Minute" : "Minutes")}");
        }

        return string.Join(' ', parts);
    }
}
