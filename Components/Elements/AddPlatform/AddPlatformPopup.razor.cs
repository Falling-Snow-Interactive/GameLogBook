using GameLogBook.Models.Companies;
using GameLogBook.Models.Games;
using GameLogBook.Models.Platforms;
using GameLogBook.Services;
using IGDB;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using IgdbCompany = IGDB.Models.Company;
using IgdbGame = IGDB.Models.Game;
using IgdbPlatform = IGDB.Models.Platform;
using IgdbPlatformVersion = IGDB.Models.PlatformVersion;

namespace GameLogBook.Components.Elements.AddPlatform;

public partial class AddPlatformPopup : ComponentBase
{
    [Inject]
    protected IgdbClientProvider IgdbClientProvider { get; set; } = null!;

    private string platformName = string.Empty;
    private DateOnly? releaseDate;
    private long igdbId;
    private HashSet<int> selectedGameIds = [];
    private HashSet<int> selectedManufacturerIds = [];
    private string selectedManufacturerCompanyId = string.Empty;

    private bool isSearching;
    private string searchInput = string.Empty;
    private string? searchErrorMessage;
    private List<PlatformSearchResult> searchResults = [];
    private CancellationTokenSource? searchCancellationTokenSource;

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<Platform> OnPlatformSelected { get; set; }

    [Parameter]
    public IReadOnlyList<Game> Games { get; set; } = [];

    [Parameter]
    public IReadOnlyList<Company> Companies { get; set; } = [];

    private IReadOnlyList<Company> AvailableManufacturerCompanies =>
        Companies
            .Where(company => !selectedManufacturerIds.Contains(company.Id))
            .OrderBy(company => company.Name)
            .ToList();

    private IReadOnlyList<Company> SelectedManufacturerCompanies =>
        Companies
            .Where(company => selectedManufacturerIds.Contains(company.Id))
            .OrderBy(company => company.Name)
            .ToList();

    private async Task HandleClose()
    {
        searchCancellationTokenSource?.Cancel();
        await OnClose.InvokeAsync();
    }

    private async Task HandleSearchInput(ChangeEventArgs args)
    {
        searchInput = args.Value?.ToString() ?? string.Empty;
        string trimmedSearchInput = searchInput.Trim();

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
            await SearchPlatforms(trimmedSearchInput, searchCancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task HandleSearch()
    {
        searchCancellationTokenSource?.Cancel();
        await SearchPlatforms(searchInput.Trim(), CancellationToken.None);
    }

    private async Task HandleSearchKeyDown(KeyboardEventArgs args)
    {
        if (args.Key is "Enter" or "NumpadEnter")
        {
            searchCancellationTokenSource?.Cancel();
            await SearchPlatforms(searchInput.Trim(), CancellationToken.None);
        }
    }

    private async Task SearchPlatforms(string trimmedSearchInput, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(trimmedSearchInput))
        {
            searchResults.Clear();
            return;
        }

        if (!IgdbClientProvider.IsConfigured)
        {
            searchErrorMessage = "Search unavailable: IGDB credentials are not configured.";
            searchResults.Clear();
            return;
        }

        isSearching = true;
        searchErrorMessage = null;

        try
        {
            string escapedSearchInput = trimmedSearchInput
                                        .Replace("\\", "\\\\")
                                        .Replace("\"", "\\\"");

            IgdbPlatform[] igdbResults = await IgdbClientProvider
                                           .GetClient()
                                           .QueryAsync<IgdbPlatform>(
                                                                     IGDBClient.Endpoints.Platforms,
                                                                     query: $"""
                                                                             fields id, name,
                                                                                    versions.main_manufacturer.company.id,
                                                                                    versions.companies.company.id,
                                                                                    versions.companies.manufacturer,
                                                                                    versions.platform_version_release_dates.date;
                                                                             where name ~ *"{escapedSearchInput}"*;
                                                                             limit 10;
                                                                             """);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            List<PlatformSearchProjection> platformProjections = igdbResults
                                                                  .Select(ToPlatformProjection)
                                                                  .ToList();
            Dictionary<long, string> manufacturerNames = await GetManufacturerNames(
                platformProjections
                    .SelectMany(projection => projection.ManufacturerCompanyIds)
                    .Distinct()
                    .ToArray());

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            searchResults = platformProjections
                            .Select(projection => new PlatformSearchResult(
                                projection.Platform,
                                projection.ManufacturerCompanyIds
                                          .Where(manufacturerNames.ContainsKey)
                                          .Select(manufacturerCompanyId => manufacturerNames[manufacturerCompanyId])
                                          .Distinct(StringComparer.OrdinalIgnoreCase)
                                          .OrderBy(name => name)
                                          .ToArray()))
                            .ToList();
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
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                isSearching = false;
            }
        }
    }

    private async Task HandlePlatformSelected(PlatformSearchResult result)
    {
        Platform platform = result.Platform;
        igdbId = platform.IgdbId;
        platformName = platform.Name;
        releaseDate = platform.ReleaseDate;
        searchInput = platform.Name;
        searchResults.Clear();
        searchErrorMessage = null;
        selectedManufacturerIds = GetMatchingLocalManufacturerIds(result.ManufacturerNames);
        selectedManufacturerCompanyId = string.Empty;

        await PopulateSelectedGames(platform.IgdbId);
    }

    private async Task HandleSavePlatform()
    {
        Platform platform = new()
                            {
                                IgdbId = igdbId,
                                Name = platformName.Trim(),
                                ReleaseDate = releaseDate,
                                ManufacturerIds = selectedManufacturerIds
                                                  .OrderBy(companyId => companyId)
                                                  .ToArray(),
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

    private void HandleManufacturerSelected(ChangeEventArgs args)
    {
        selectedManufacturerCompanyId = args.Value?.ToString() ?? string.Empty;

        if (int.TryParse(selectedManufacturerCompanyId, out int companyId))
        {
            selectedManufacturerIds.Add(companyId);
        }

        selectedManufacturerCompanyId = string.Empty;
    }

    private void RemoveManufacturer(int companyId)
    {
        selectedManufacturerIds.Remove(companyId);
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

    private HashSet<int> GetMatchingLocalManufacturerIds(IEnumerable<string> manufacturerNames)
    {
        HashSet<string> normalizedManufacturerNames = manufacturerNames
                                                      .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Companies
               .Where(company => normalizedManufacturerNames.Contains(company.Name))
               .Select(company => company.Id)
               .ToHashSet();
    }

    private static string GetPlatformSummary(PlatformSearchResult result)
    {
        string manufacturerSummary = result.ManufacturerNames.Length == 0
                                         ? "Unknown manufacturer"
                                         : string.Join(", ", result.ManufacturerNames);

        string releaseSummary = result.Platform.ReleaseDate.HasValue
                                    ? result.Platform.ReleaseDate.Value.ToString("yyyy-MM-dd")
                                    : "Unknown release date";

        return $"{manufacturerSummary} · {releaseSummary}";
    }

    private async Task<Dictionary<long, string>> GetManufacturerNames(long[] companyIds)
    {
        if (companyIds.Length == 0)
        {
            return [];
        }

        string companyIdsFilter = string.Join(",", companyIds);

        IgdbCompany[] companies = await IgdbClientProvider
                                  .GetClient()
                                  .QueryAsync<IgdbCompany>(
                                                             IGDBClient.Endpoints.Companies,
                                                             query: $"""
                                                                     fields id, name;
                                                                     where id = ({companyIdsFilter});
                                                                     limit {companyIds.Length};
                                                                     """);

        return companies
               .Where(company => company.Id.HasValue
                                 && !string.IsNullOrWhiteSpace(company.Name))
               .ToDictionary(company => company.Id!.Value,
                             company => company.Name!);
    }

    private static PlatformSearchProjection ToPlatformProjection(IgdbPlatform igdbPlatform)
    {
        List<IgdbPlatformVersion> versions = igdbPlatform.Versions?.Values?.ToList() ?? [];

        return new PlatformSearchProjection(
            new Platform
            {
                IgdbId = igdbPlatform.Id ?? 0,
                Name = igdbPlatform.Name ?? string.Empty,
                ReleaseDate = GetReleaseDate(versions)
            },
            GetManufacturerCompanyIds(versions));
    }

    private static long[] GetManufacturerCompanyIds(IEnumerable<IgdbPlatformVersion> versions)
    {
        return versions
               .SelectMany(version =>
               {
                   IEnumerable<long?> mainManufacturerCompanyIds =
                       [version.MainManufacturer?.Value?.Company?.Id];

                   IEnumerable<long?> manufacturerCompanyIds =
                       version.Companies?.Values?
                              .Where(company => company.Manufacturer is true)
                              .Select(company => company.Company?.Id)
                       ?? [];

                   return mainManufacturerCompanyIds.Concat(manufacturerCompanyIds);
               })
               .Where(companyId => companyId.HasValue)
               .Select(companyId => companyId!.Value)
               .Distinct()
               .ToArray();
    }

    private static DateOnly? GetReleaseDate(IEnumerable<IgdbPlatformVersion> versions)
    {
        DateTimeOffset? firstReleaseDate = versions
                                           .SelectMany(version => version.PlatformVersionReleaseDates?.Values ?? [])
                                           .Where(release => release.Date.HasValue)
                                           .Select(release => release.Date!.Value)
                                           .OrderBy(date => date)
                                           .FirstOrDefault();

        return firstReleaseDate.HasValue
                   ? DateOnly.FromDateTime(firstReleaseDate.Value.UtcDateTime)
                   : null;
    }

    private static bool IsAuthenticationFailure(Exception exception)
    {
        return exception.Message.Contains("id.twitch.tv/oauth2/token", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record PlatformSearchProjection(Platform Platform, long[] ManufacturerCompanyIds);

    public sealed record PlatformSearchResult(Platform Platform, string[] ManufacturerNames);
}
