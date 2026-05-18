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
    private string manufacturer = string.Empty;
    private DateOnly? releaseDate;
    private long igdbId;
    private HashSet<int> selectedGameIds = [];

    private bool isSearching;
    private string searchInput = string.Empty;
    private string? searchErrorMessage;
    private List<Platform> searchResults = [];
    private CancellationTokenSource? searchCancellationTokenSource;

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<Platform> OnPlatformSelected { get; set; }

    [Parameter]
    public IReadOnlyList<Game> Games { get; set; } = [];

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
                                                                                    versions.main_manufacturer.company.name,
                                                                                    versions.companies.company.name,
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
                    .Where(projection => projection.ManufacturerCompanyId.HasValue)
                    .Select(projection => projection.ManufacturerCompanyId!.Value)
                    .Distinct()
                    .ToArray());

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            searchResults = platformProjections
                            .Select(projection =>
                            {
                                if (projection.ManufacturerCompanyId.HasValue
                                    && manufacturerNames.TryGetValue(projection.ManufacturerCompanyId.Value, out string? manufacturerName))
                                {
                                    projection.Platform.Manufacturer = manufacturerName;
                                }

                                return projection.Platform;
                            })
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

    private async Task HandlePlatformSelected(Platform platform)
    {
        igdbId = platform.IgdbId;
        platformName = platform.Name;
        manufacturer = platform.Manufacturer ?? string.Empty;
        releaseDate = platform.ReleaseDate;
        searchInput = platform.Name;
        searchResults.Clear();
        searchErrorMessage = null;

        await PopulateSelectedGames(platform.IgdbId);
    }

    private async Task HandleSavePlatform()
    {
        Platform platform = new()
                            {
                                IgdbId = igdbId,
                                Name = platformName.Trim(),
                                ReleaseDate = releaseDate,
                                Manufacturer = string.IsNullOrWhiteSpace(manufacturer)
                                                   ? null
                                                   : manufacturer.Trim(),
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

    private static string GetPlatformSummary(Platform platform)
    {
        string manufacturerSummary = string.IsNullOrWhiteSpace(platform.Manufacturer)
                                         ? "Unknown manufacturer"
                                         : platform.Manufacturer;

        string releaseSummary = platform.ReleaseDate.HasValue
                                    ? platform.ReleaseDate.Value.ToString("yyyy-MM-dd")
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
                Manufacturer = GetManufacturerName(versions),
                ReleaseDate = GetReleaseDate(versions)
            },
            GetManufacturerCompanyId(versions));
    }

    private static string? GetManufacturerName(IEnumerable<IgdbPlatformVersion> versions)
    {
        foreach (IgdbPlatformVersion version in versions)
        {
            string? mainManufacturer = version.MainManufacturer?.Value?.Company?.Value?.Name;

            if (IsUsableManufacturerName(mainManufacturer))
            {
                return mainManufacturer;
            }

            string? manufacturer = version.Companies?.Values?
                                          .FirstOrDefault(company => company.Manufacturer is true)
                                          ?.Company?.Value?.Name;

            if (IsUsableManufacturerName(manufacturer))
            {
                return manufacturer;
            }
        }

        return null;
    }

    private static long? GetManufacturerCompanyId(IEnumerable<IgdbPlatformVersion> versions)
    {
        foreach (IgdbPlatformVersion version in versions)
        {
            long? mainManufacturerCompanyId = version.MainManufacturer?.Value?.Company?.Id;

            if (mainManufacturerCompanyId.HasValue)
            {
                return mainManufacturerCompanyId.Value;
            }

            long? manufacturerCompanyId = version.Companies?.Values?
                                                 .FirstOrDefault(company => company.Manufacturer is true)
                                                 ?.Company?.Id;

            if (manufacturerCompanyId.HasValue)
            {
                return manufacturerCompanyId.Value;
            }
        }

        return null;
    }

    private static bool IsUsableManufacturerName(string? manufacturerName)
    {
        return !string.IsNullOrWhiteSpace(manufacturerName)
               && !long.TryParse(manufacturerName, out _);
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

    private sealed record PlatformSearchProjection(Platform Platform, long? ManufacturerCompanyId);
}
