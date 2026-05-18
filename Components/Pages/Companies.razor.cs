using GameLogBook.Data;
using GameLogBook.Models.Companies;
using GameLogBook.Models.Games;
using GameLogBook.Services;
using IGDB;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using IgdbCompany = IGDB.Models.Company;
using IgdbCompanyLogo = IGDB.Models.CompanyLogo;
using IgdbGame = IGDB.Models.Game;

namespace GameLogBook.Components.Pages;

public partial class Companies : CollectionPageBase<Company>
{
    [Inject]
    private IgdbClientProvider IgdbClientProvider { get; set; } = null!;

    private List<Game> games = [];
    private HashSet<int> selectedGameIds = [];

    private const int SearchPageSize = 10;

    private bool isSearching;
    private bool isLoadingMore;
    private bool canLoadMore;
    private int searchOffset;
    private string searchInput = string.Empty;
    private string? searchErrorMessage;
    private List<Company> searchResults = [];
    private CancellationTokenSource? searchCancellationTokenSource;
    private string newCompanyName = string.Empty;
    private string newCompanyCoverUrl = string.Empty;
    private bool newCompanyIsPublisher;
    private bool newCompanyIsDeveloper;

    protected override DbSet<Company> EntitySet => DbContext.Companies;

    protected override string GetSortKey(Company item)
    {
        return item.Name;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        games = await DbContext.Games
                               .OrderBy(game => game.Name)
                               .ToListAsync();
    }

    protected override void CloseAddPopup()
    {
        base.CloseAddPopup();
        searchCancellationTokenSource?.Cancel();
        ResetForm();
    }

    private async Task HandleSearchInput(ChangeEventArgs args)
    {
        searchInput = args.Value?.ToString() ?? string.Empty;
        string trimmedSearchInput = searchInput.Trim();

        searchOffset = 0;
        canLoadMore = false;

        searchCancellationTokenSource?.Cancel();
        searchCancellationTokenSource = new CancellationTokenSource();

        if (trimmedSearchInput.Length < 2)
        {
            searchResults.Clear();
            searchErrorMessage = null;
            return;
        }

        try
        {
            await Task.Delay(300, searchCancellationTokenSource.Token);
            await SearchCompanies(trimmedSearchInput, searchCancellationTokenSource.Token, appendResults: false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task HandleSearch()
    {
        searchCancellationTokenSource?.Cancel();
        searchOffset = 0;
        canLoadMore = false;
        await SearchCompanies(searchInput.Trim(), CancellationToken.None, appendResults: false);
    }

    private async Task HandleLoadMore()
    {
        if (!canLoadMore || isSearching || isLoadingMore)
        {
            return;
        }

        searchCancellationTokenSource?.Cancel();
        await SearchCompanies(searchInput.Trim(), CancellationToken.None, appendResults: true);
    }

    private async Task HandleSearchKeyDown(KeyboardEventArgs args)
    {
        if (args.Key is "Enter" or "NumpadEnter")
        {
            searchCancellationTokenSource?.Cancel();
            searchOffset = 0;
            canLoadMore = false;
            await SearchCompanies(searchInput.Trim(), CancellationToken.None, appendResults: false);
        }
    }

    private async Task SearchCompanies(string trimmedSearchInput, CancellationToken cancellationToken, bool appendResults)
    {
        if (string.IsNullOrWhiteSpace(trimmedSearchInput))
        {
            searchResults.Clear();
            canLoadMore = false;
            return;
        }

        if (!IgdbClientProvider.IsConfigured)
        {
            searchErrorMessage = "Search unavailable: IGDB credentials are not configured.";
            searchResults.Clear();
            canLoadMore = false;
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

        searchErrorMessage = null;

        try
        {
            string escapedSearchInput = trimmedSearchInput
                                        .Replace("\\", "\\\\")
                                        .Replace("\"", "\\\"");

            IgdbCompany[] igdbResults = await IgdbClientProvider
                                         .GetClient()
                                         .QueryAsync<IgdbCompany>(
                                                               IGDBClient.Endpoints.Companies,
                                                               query: $"""
                                                                       fields id, name, developed.id, published.id, logo.url;
                                                                       where name ~ *"{escapedSearchInput}"*;
                                                                       limit {SearchPageSize};
                                                                       offset {searchOffset};
                                                                       """);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            List<Company> newResults = igdbResults
                                       .Select(ToLocalCompany)
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
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            searchErrorMessage = IsAuthenticationFailure(exception)
                                     ? "Search failed: IGDB credentials were rejected. Check the configured client ID and client secret."
                                     : $"Search failed: {exception.Message}";
            searchResults.Clear();
            canLoadMore = false;
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                isSearching = false;
                isLoadingMore = false;
            }
        }
    }

    private void HandleCompanySelected(Company company)
    {
        newCompanyName = company.Name;
        newCompanyCoverUrl = company.CoverUrl ?? string.Empty;
        newCompanyIsDeveloper = company.IsDeveloper;
        newCompanyIsPublisher = company.IsPublisher;
        selectedGameIds = company.GameIds.ToHashSet();
        searchInput = company.Name;
        searchResults.Clear();
        canLoadMore = false;
        searchErrorMessage = null;
    }

    private async Task AddCompany()
    {
        Company company = new()
                          {
                              Name = newCompanyName.Trim(),
                              CoverUrl = string.IsNullOrWhiteSpace(newCompanyCoverUrl)
                                             ? null
                                             : newCompanyCoverUrl.Trim(),
                              IsDeveloper = newCompanyIsDeveloper,
                              IsPublisher = newCompanyIsPublisher,
                              GameIds = selectedGameIds
                                        .OrderBy(gameId => gameId)
                                        .ToArray()
                          };

        await AddItemAsync(company);
        CloseAddPopup();
    }

    private async Task HandleRemove(Company company)
    {
        await RemoveItemAsync(company);
    }

    private string GetGameName(int gameId)
    {
        return games.FirstOrDefault(game => game.Id == gameId)?.Name ?? $"Game #{gameId}";
    }

    private static string GetCompanyRoleSummary(Company company)
    {
        if (company.IsDeveloper && company.IsPublisher)
        {
            return "Developer · Publisher";
        }

        if (company.IsDeveloper)
        {
            return "Developer";
        }

        if (company.IsPublisher)
        {
            return "Publisher";
        }

        return "No matching local games yet";
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

    private void ResetForm()
    {
        searchInput = string.Empty;
        searchErrorMessage = null;
        searchResults.Clear();
        isSearching = false;
        isLoadingMore = false;
        canLoadMore = false;
        searchOffset = 0;
        newCompanyName = string.Empty;
        newCompanyCoverUrl = string.Empty;
        newCompanyIsPublisher = false;
        newCompanyIsDeveloper = false;
        selectedGameIds.Clear();
    }

    private Company ToLocalCompany(IgdbCompany igdbCompany)
    {
        HashSet<long> developedGameIds = GetIgdbGameIds(igdbCompany.Developed);
        HashSet<long> publishedGameIds = GetIgdbGameIds(igdbCompany.Published);
        HashSet<long> workedOnGameIds = developedGameIds
                                        .Concat(publishedGameIds)
                                        .ToHashSet();

        return new Company
               {
                   Name = igdbCompany.Name ?? string.Empty,
                   CoverUrl = ToLocalCoverUrl(igdbCompany.Logo?.Value),
                   IsDeveloper = developedGameIds.Count > 0,
                   IsPublisher = publishedGameIds.Count > 0,
                   GameIds = games
                             .Where(game => workedOnGameIds.Contains(game.IgdbId))
                             .Select(game => game.Id)
                             .OrderBy(gameId => gameId)
                             .ToArray()
               };
    }

    private static HashSet<long> GetIgdbGameIds(IdentitiesOrValues<IgdbGame>? igdbGames)
    {
        return igdbGames?.Values?
                        .Where(game => game.Id.HasValue)
                        .Select(game => game.Id!.Value)
                        .ToHashSet()
               ?? [];
    }

    private static string? ToLocalCoverUrl(IgdbCompanyLogo? igdbCompanyLogo)
    {
        if (igdbCompanyLogo?.Url is null)
        {
            return null;
        }

        return igdbCompanyLogo.Url.StartsWith("//")
                   ? $"https:{igdbCompanyLogo.Url}"
                   : igdbCompanyLogo.Url;
    }

    private static bool IsAuthenticationFailure(Exception exception)
    {
        return exception.Message.Contains("id.twitch.tv/oauth2/token", StringComparison.OrdinalIgnoreCase);
    }
}
