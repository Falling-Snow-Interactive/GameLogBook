using IGDB;
using IGDB.Models;
using GameLogBook.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Company = GameLogBook.Models.Companies.Company;
using Cover = GameLogBook.Models.Games.Cover;
using Game = GameLogBook.Models.Games.Game;
using IgdbGame = IGDB.Models.Game;
using IgdbCover = IGDB.Models.Cover;

namespace GameLogBook.Components.Elements.AddGame;

public partial class AddGamePopup
{
    [Inject]
    protected IgdbClientProvider IgdbClientProvider { get; set; } = null!;

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<Game> OnGameSelected { get; set; }

    private const int SearchPageSize = 10;

    private string searchInput = string.Empty;
    private List<Game> searchResults = [];
    private bool isSearching;
    private bool isLoadingMore;
    private bool canLoadMore;
    private int searchOffset;
    private string? errorMessage;
    
    private List<Company> companies = [];
    private int? selectedDeveloperCompanyId;
    private int? selectedPublisherCompanyId;

    private string gameName = string.Empty;
    private long igdbId;
    private string? developer = string.Empty;
    private string? publisher = string.Empty;
    private DateOnly? releaseDate;
    private string coverUrl = string.Empty;
    private string summary = string.Empty;

    private async Task HandleClose()
    {
        await OnClose.InvokeAsync();
    }

    private async Task HandleSearch()
    {
        searchOffset = 0;
        canLoadMore = false;
        searchResults.Clear();

        await SearchGamesAsync(appendResults: false);
    }

    private async Task HandleLoadMore()
    {
        if (!canLoadMore || isSearching || isLoadingMore)
        {
            return;
        }

        await SearchGamesAsync(appendResults: true);
    }

    private async Task SearchGamesAsync(bool appendResults)
    {
        string trimmedSearchInput = searchInput.Trim();

        if (string.IsNullOrWhiteSpace(trimmedSearchInput))
        {
            return;
        }

        if (appendResults)
        {
            isLoadingMore = true;
        }
        else
        {
            isSearching = true;
        }

        errorMessage = null;

        if (!IgdbClientProvider.IsConfigured)
        {
            errorMessage = "Search unavailable: IGDB credentials are not configured.";
            isSearching = false;
            isLoadingMore = false;
            return;
        }

        try
        {
            string escapedSearchInput = trimmedSearchInput
                                        .Replace("\\", "\\\\")
                                        .Replace("\"", "\\\"");

            IgdbGame[] igdbResults = await IgdbClientProvider
                                           .GetClient()
                                           .QueryAsync<IgdbGame>(
                                                                 IGDBClient.Endpoints.Games,
                                                                 query: $"""
                                                                         search "{escapedSearchInput}";
                                                                         fields id, name, summary, first_release_date, cover.url;
                                                                         limit {SearchPageSize};
                                                                         offset {searchOffset};
                                                                         """);

            List<Game> newResults = igdbResults
                                    .Select(ToLocalGame)
                                    .ToList();

            if (appendResults)
            {
                searchResults.AddRange(newResults);
            }
            else
            {
                searchResults = newResults;
            }

            searchOffset += newResults.Count;
            canLoadMore = newResults.Count == SearchPageSize;
        }
        catch (Exception exception)
        {
            errorMessage = IsAuthenticationFailure(exception)
                               ? "Search failed: IGDB credentials were rejected. Check the configured client ID and client secret."
                               : $"Search failed: {exception.Message}";
        }
        finally
        {
            isSearching = false;
            isLoadingMore = false;
        }
    }

    private Task HandleGameSelected(Game game)
    {
        igdbId = game.IgdbId;
        gameName = game.Name;
        developer = game.Developer;
        publisher = game.Publisher;
        releaseDate = game.ReleaseDate;
        coverUrl = game.Cover?.Url ?? string.Empty;
        summary = game.Summary ?? string.Empty;

        searchInput = game.Name;
        searchResults.Clear();

        return Task.CompletedTask;
    }

    private async Task HandleSaveGame()
    {
        Game game = new()
                    {
                        IgdbId = igdbId,
                        Name = gameName.Trim(),
                        Developer = string.IsNullOrWhiteSpace(developer) ? null : developer.Trim(),
                        Publisher = string.IsNullOrWhiteSpace(publisher) ? null : publisher.Trim(),
                        ReleaseDate = releaseDate,
                        Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim(),
                        Cover = string.IsNullOrWhiteSpace(coverUrl)
                                    ? null
                                    : new Cover
                                      {
                                          Url = coverUrl.Trim()
                                      }
                    };

        await OnGameSelected.InvokeAsync(game);
    }

    private async Task HandleKeyDown(KeyboardEventArgs args)
    {
        if (args.Key is "Enter" or "NumpadEnter")
        {
            await HandleSearch();
        }
    }

    private void HandleSearchInput(ChangeEventArgs args)
    {
        searchInput = args.Value?.ToString() ?? string.Empty;
    }

    private static Game ToLocalGame(IgdbGame igdbGame)
    {
        string dev = "";
        string pub = "";
        
        if (igdbGame.InvolvedCompanies != null)
        {
            foreach (InvolvedCompany? c in igdbGame.InvolvedCompanies.Values)
            {
                if (c.Developer.HasValue)
                {
                    dev = c.Company.Value.Name;
                }

                if (c.Publisher.HasValue)
                {
                    pub = c.Company.Value.Name;
                }
            }
        }

        return new Game
               {
                   IgdbId = Convert.ToInt32(igdbGame.Id ?? 0),
                   Name = igdbGame.Name,
                   Summary = igdbGame.Summary,
                   Developer = dev,
                   Publisher = pub,
                   ReleaseDate = ToDateOnly(igdbGame.FirstReleaseDate?.ToUnixTimeSeconds()),
                   Cover = ToLocalCover(igdbGame.Cover.Value)
               };
    }

    private static Cover? ToLocalCover(IgdbCover? igdbCover)
    {
        if (igdbCover?.Url is null)
        {
            return null;
        }

        return new Cover
               {
                   Url = igdbCover.Url.StartsWith("//")
                             ? $"https:{igdbCover.Url}"
                             : igdbCover.Url
               };
    }

    private static DateOnly? ToDateOnly(long? unixTime)
    {
        return unixTime.HasValue
                   ? DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(unixTime.Value).UtcDateTime)
                   : null;
    }

    private static bool IsAuthenticationFailure(Exception exception)
    {
        return exception.Message.Contains("id.twitch.tv/oauth2/token", StringComparison.OrdinalIgnoreCase);
    }
}
