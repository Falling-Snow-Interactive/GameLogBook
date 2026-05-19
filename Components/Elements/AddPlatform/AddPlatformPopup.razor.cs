using GameLogBook.Components.Elements.IGDBSearch;
using GameLogBook.Models.Games;
using GameLogBook.Models.Platforms;
using GameLogBook.Services;
using IGDB;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using IgdbGame = IGDB.Models.Game;

namespace GameLogBook.Components.Elements.AddPlatform;

public partial class AddPlatformPopup : ComponentBase
{
    [Inject]
    protected IgdbClientProvider IgdbClientProvider { get; set; } = null!;

    private string platformName = string.Empty;
    private DateOnly? releaseDate;
    private long igdbId;
    private HashSet<int> selectedGameIds = [];
    private HashSet<int> companyIds = [];

    private string? searchErrorMessage;

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<Platform> OnPlatformSelected { get; set; }

    [Parameter]
    public IReadOnlyList<Game> Games { get; set; } = [];

    private async Task HandleClose()
    {
        await OnClose.InvokeAsync();
    }

    private async Task HandlePlatformSelected(IgdbSearchPlatformResult result)
    {
        Platform platform = result.Platform;
        igdbId = platform.IgdbId;
        platformName = platform.Name;
        releaseDate = platform.ReleaseDate;
        searchErrorMessage = null;
        // selectedCompanyIds = GetMatchingLocalManufacturerIds(result.ManufacturerNames);
        // selectedCompanyId = string.Empty;

        await PopulateSelectedGames(platform.IgdbId);
    }

    private async Task HandleSavePlatform()
    {
        Platform platform = new()
                            {
                                IgdbId = igdbId,
                                Name = platformName.Trim(),
                                ReleaseDate = releaseDate,
                                // ManufacturerIds = selectedCompanyIds
                                //                   .OrderBy(companyId => companyId)
                                //                   .ToArray(),
                                GameIds = selectedGameIds
                                          .OrderBy(gameId => gameId)
                                          .ToArray()
                            };

        await OnPlatformSelected.InvokeAsync(platform);
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

    private async Task PopulateSelectedGames(long platformIgdbId)
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
}
