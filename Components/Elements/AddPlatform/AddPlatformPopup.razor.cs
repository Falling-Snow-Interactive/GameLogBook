using GameLogBook.Components.Elements.IGDBSearch;
using GameLogBook.Models.Companies;
using GameLogBook.Models.Games;
using GameLogBook.Models.Platforms;
using GameLogBook.Services;
using IGDB;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using IgdbGame = IGDB.Models.Game;
using PlatformModel = GameLogBook.Models.Platforms.Platform;

namespace GameLogBook.Components.Elements.AddPlatform;

public partial class AddPlatformPopup : ComponentBase
{
    [Inject]
    protected IgdbClientProvider IgdbClientProvider { get; set; } = null!;

    private PlatformModel? previousInitialPlatform;
    private string platformName = string.Empty;
    private string abbreviation = string.Empty;
    private string platformCoverUrl = string.Empty;
    private DateOnly? releaseDate;
    private long? igdbId;
    private HashSet<int> selectedGameIds = [];
    private HashSet<int> companyIds = [];

    private string? searchErrorMessage;

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<PlatformModel> OnPlatformSelected { get; set; }

    [Parameter]
    public IReadOnlyList<Game> Games { get; set; } = [];

    [Parameter]
    public IReadOnlyList<Company> Companies { get; set; } = [];

    [Parameter]
    public PlatformModel? InitialPlatform { get; set; }

    private string PopupTitle => InitialPlatform is null ? "Add Platform" : "Edit Platform";

    private string SaveButtonText => InitialPlatform is null ? "Add Platform" : "Save Changes";

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(previousInitialPlatform, InitialPlatform))
        {
            return;
        }

        previousInitialPlatform = InitialPlatform;

        if (InitialPlatform is null)
        {
            ResetForm();
            return;
        }

        LoadPlatform(InitialPlatform);
    }

    private async Task HandlePlatformSelected(IgdbSearchPlatformResult result)
    {
        PlatformModel platform = result.Platform;
        igdbId = platform.IgdbId;
        platformName = platform.Name;
        platformCoverUrl = platform.CoverUrl ?? string.Empty;
        releaseDate = platform.ReleaseDate;
        searchErrorMessage = null;
        // selectedCompanyIds = GetMatchingLocalManufacturerIds(result.ManufacturerNames);
        // selectedCompanyId = string.Empty;

        await PopulateSelectedGames(platform.IgdbId);
    }

    private async Task HandleSavePlatform()
    {
        var initialPlatform = InitialPlatform?.ID ?? 0;
        var name = platformName.Trim();
        var cover = string.IsNullOrWhiteSpace(platformCoverUrl)
                        ? null
                        : platformCoverUrl.Trim();
        var manufacturerIds = companyIds
                              .OrderBy(companyId => companyId)
                              .ToArray();
        var gameIds = selectedGameIds
                      .OrderBy(gameId => gameId)
                      .ToArray();

        PlatformModel platform = new(igdbId, name, abbreviation, cover, releaseDate, manufacturerIds, gameIds);

        await OnPlatformSelected.InvokeAsync(platform);
    }

    private Task HandleCompanyIdsChanged(HashSet<int> updatedCompanyIds)
    {
        companyIds = updatedCompanyIds;
        return Task.CompletedTask;
    }

    private void ToggleGameSelection(int gameId, ChangeEventArgs args)
    {
        if (args.Value is true)
        {
            selectedGameIds.Add(gameId);
            return;
        }

        selectedGameIds.Remove(gameId);
    }

    private async Task PopulateSelectedGames(long? platformIgdbId)
    {
        selectedGameIds.Clear();

        if (platformIgdbId <= 0 || Games.Count == 0 || !IgdbClientProvider.IsConfigured)
        {
            return;
        }

        long[] localIgdbGameIds = Games
                                  .Where(game => game.IgdbId > 0)
                                  .Select(game => game.IgdbId)
                                  .ToArray();

        if (localIgdbGameIds.Length == 0)
        {
            return;
        }

        try
        {
            string localIgdbGameIdsFilter = string.Join(",", localIgdbGameIds);

            IgdbGame[] igdbGames = await IgdbClientProvider
                                         .GetClient()
                                         .QueryAsync<IgdbGame>(
                                                               IGDBClient.Endpoints.Games,
                                                               query: $"""
                                                                       fields id;
                                                                       where id = ({localIgdbGameIdsFilter})
                                                                             & platforms = {platformIgdbId};
                                                                       limit {localIgdbGameIds.Length};
                                                                       """);

            HashSet<long> matchedGameIds = igdbGames
                                           .Where(game => game.Id.HasValue)
                                           .Select(game => game.Id!.Value)
                                           .ToHashSet();

            selectedGameIds = Games
                              .Where(game => matchedGameIds.Contains(game.IgdbId))
                              .Select(game => game.Id)
                              .ToHashSet();
        }
        catch (Exception exception)
        {
            searchErrorMessage = IsAuthenticationFailure(exception)
                                     ? "Search failed: IGDB credentials were rejected. Check the configured client ID and client secret."
                                     : $"Could not load linked games: {exception.Message}";
        }
    }

    private static bool IsAuthenticationFailure(Exception exception)
    {
        return exception.Message.Contains("id.twitch.tv/oauth2/token", StringComparison.OrdinalIgnoreCase);
    }

    private void LoadPlatform(PlatformModel platform)
    {
        igdbId = platform.IgdbId;
        platformName = platform.Name;
        platformCoverUrl = platform.CoverUrl ?? string.Empty;
        releaseDate = platform.ReleaseDate;
        selectedGameIds = platform.GameIds.ToHashSet();
        companyIds = platform.ManufacturerIds.ToHashSet();
        searchErrorMessage = null;
    }

    private void ResetForm()
    {
        platformName = string.Empty;
        platformCoverUrl = string.Empty;
        releaseDate = null;
        igdbId = 0;
        selectedGameIds = [];
        companyIds = [];
        searchErrorMessage = null;
    }
}
