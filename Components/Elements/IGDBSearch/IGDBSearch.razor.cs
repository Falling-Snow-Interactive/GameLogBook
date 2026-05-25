using GameLogBook.Services;
using GameLogBook.Models.Games;
using IGDB;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using IgdbCompany = IGDB.Models.Company;
using IgdbCompanyLogo = IGDB.Models.CompanyLogo;
using IgdbCover = IGDB.Models.Cover;
using IgdbGame = IGDB.Models.Game;
using IgdbInvolvedCompany = IGDB.Models.InvolvedCompany;
using IgdbPlatform = IGDB.Models.Platform;
using IgdbPlatformVersion = IGDB.Models.PlatformVersion;
using IgdbPlatformVersionCompany = IGDB.Models.PlatformVersionCompany;
using Image = GameLogBook.Models.Games.Image;
using LocalCompany = GameLogBook.Models.Companies.Company;
using LocalGame = GameLogBook.Models.Games.Game;
using LocalPlatform = GameLogBook.Models.Platforms.Platform;

namespace GameLogBook.Components.Elements.IGDBSearch;

public partial class IGDBSearch : ComponentBase, IDisposable
{
    private const int MinSearchLength = 2;
    private const int DebounceDelayMilliseconds = 300;
    private const int CompanyCandidateLimit = 500;

    private string searchInput = string.Empty;
    private string? searchErrorMessage;
    private string activeSearchText = string.Empty;
    private bool isSearching;
    private bool isLoadingMore;
    private bool canLoadMore;
    private bool hasSearched;
    private int searchOffset;
    private CancellationTokenSource? searchCancellationTokenSource;

    private List<LocalGame> gameResults = [];
    private List<IgdbSearchPlatformResult> platformResults = [];
    private List<LocalCompany> companyResults = [];
    private List<LocalCompany> companyResultCandidates = [];

    [Inject]
    private IGDBClientProvider IgdbClientProvider { get; set; } = null!;

    [Parameter]
    public string Label { get; set; } = "Search IGDB";

    [Parameter]
    public string Placeholder { get; set; } = string.Empty;

    [Parameter]
    public string Value { get; set; } = string.Empty;

    [Parameter]
    public IgdbSearchFor SearchFor { get; set; } = IgdbSearchFor.Games;

    [Parameter]
    public int MaxResults { get; set; } = 100;

    [Parameter]
    public IReadOnlyList<LocalGame> LocalGames { get; set; } = [];

    [Parameter]
    public IReadOnlyList<LocalCompany> LocalCompanies { get; set; } = [];

    [Parameter]
    public EventCallback<LocalGame> OnGameSelected { get; set; }

    [Parameter]
    public EventCallback<IgdbSearchPlatformResult> OnPlatformSelected { get; set; }

    [Parameter]
    public EventCallback<LocalCompany> OnCompanySelected { get; set; }

    private bool HasResults => SearchFor switch
                               {
                                   IgdbSearchFor.Games => gameResults.Count > 0,
                                   IgdbSearchFor.Platforms => platformResults.Count > 0,
                                   IgdbSearchFor.Companies => companyResults.Count > 0,
                                   _ => false
                               };

    private bool ShouldShowDropdown => isSearching
                                       || isLoadingMore
                                       || hasSearched
                                       || !string.IsNullOrWhiteSpace(searchErrorMessage)
                                       || HasResults;

    protected override void OnParametersSet()
    {
        if (!string.IsNullOrWhiteSpace(Value) && searchInput != Value)
        {
            searchInput = Value;
        }
    }

    public void Dispose()
    {
        searchCancellationTokenSource?.Cancel();
        searchCancellationTokenSource?.Dispose();
    }

    private async Task HandleSearchInput(ChangeEventArgs args)
    {
        searchInput = args.Value?.ToString() ?? string.Empty;
        searchOffset = 0;
        canLoadMore = false;
        hasSearched = false;
        searchErrorMessage = null;
        activeSearchText = searchInput.Trim();
        ClearResults();

        searchCancellationTokenSource?.Cancel();
        searchCancellationTokenSource?.Dispose();
        searchCancellationTokenSource = new CancellationTokenSource();

        if (activeSearchText.Length < MinSearchLength)
        {
            return;
        }

        try
        {
            await Task.Delay(DebounceDelayMilliseconds, searchCancellationTokenSource.Token);
            await SearchAsync(activeSearchText, searchCancellationTokenSource.Token, appendResults: false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task HandleSearchKeyDown(KeyboardEventArgs args)
    {
        if (args.Key is not ("Enter" or "NumpadEnter"))
        {
            return;
        }

        await searchCancellationTokenSource?.CancelAsync()!;
        searchOffset = 0;
        canLoadMore = false;
        ClearResults();

        await SearchAsync(searchInput.Trim(), CancellationToken.None, appendResults: false);
    }

    private async Task HandleLoadMore()
    {
        if (!canLoadMore || isSearching || isLoadingMore)
        {
            return;
        }

        await searchCancellationTokenSource?.CancelAsync()!;
        await SearchAsync(activeSearchText, CancellationToken.None, appendResults: true);
    }

    private async Task SearchAsync(string trimmedSearchInput, CancellationToken cancellationToken, bool appendResults)
    {
        if (string.IsNullOrWhiteSpace(trimmedSearchInput))
        {
            ClearResults();
            canLoadMore = false;
            hasSearched = false;
            return;
        }

        if (!IgdbClientProvider.IsConfigured)
        {
            searchErrorMessage = "Search unavailable: IGDB credentials are not configured.";
            ClearResults();
            canLoadMore = false;
            hasSearched = true;
            return;
        }

        activeSearchText = trimmedSearchInput;
        searchErrorMessage = null;

        if (appendResults)
        {
            isLoadingMore = true;
        }
        else
        {
            isSearching = true;
        }

        await InvokeAsync(StateHasChanged);

        try
        {
            int resultCount = SearchFor switch
                              {
                                  IgdbSearchFor.Games => await SearchGames(trimmedSearchInput, appendResults),
                                  IgdbSearchFor.Platforms => await SearchPlatforms(trimmedSearchInput, appendResults, cancellationToken),
                                  IgdbSearchFor.Companies => await SearchCompanies(trimmedSearchInput, appendResults, cancellationToken),
                                  _ => 0
                              };

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            searchOffset += resultCount;
            canLoadMore = CanLoadMoreResults(resultCount);
            hasSearched = true;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            searchErrorMessage = IsAuthenticationFailure(exception)
                                     ? "Search failed: IGDB credentials were rejected. Check the configured client ID and client secret."
                                     : $"Search failed: {exception.Message}";
            ClearResults();
            canLoadMore = false;
            hasSearched = true;
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

    private async Task<int> SearchGames(string trimmedSearchInput, bool appendResults)
    {
        string escapedSearchInput = EscapeSearchInput(trimmedSearchInput);

        IgdbGame[] igdbResults = await IgdbClientProvider
                                       .GetClient()
                                       .QueryAsync<IgdbGame>(
                                                             IGDBClient.Endpoints.Games,
                                                                     query: $"""
                                                                     search "{escapedSearchInput}";
                                                                     fields id, name, summary, first_release_date, cover.url,
                                                                            involved_companies.developer,
                                                                            involved_companies.publisher,
                                                                            involved_companies.company.id,
                                                                            involved_companies.company.name,
                                                                            involved_companies.company.logo.url;
                                                                     limit {MaxResults};
                                                                     offset {searchOffset};
                                                                     """);

        List<LocalGame> newResults = igdbResults
                                     .Select(ToLocalGame)
                                     .OrderByNameRelevance(trimmedSearchInput, game => game.Name)
                                     .ToList();

        if (appendResults)
        {
            gameResults.AddRange(newResults);
        }
        else
        {
            gameResults = newResults;
        }

        return newResults.Count;
    }

    private async Task<int> SearchPlatforms(string trimmedSearchInput, bool appendResults, CancellationToken cancellationToken)
    {
        string escapedSearchInput = EscapeSearchInput(trimmedSearchInput);

        IgdbPlatform[] igdbResults = await IgdbClientProvider
                                           .GetClient()
                                           .QueryAsync<IgdbPlatform>(IGDBClient.Endpoints.Platforms,
                                                                     query: $"""
                                                                                     fields id, name, abbreviation, platform_logo.url,
                                                                                            versions.id,
                                                                                            versions.main_manufacturer.id,
                                                                                            versions.main_manufacturer.company.id,
                                                                                            versions.main_manufacturer.company.name,
                                                                                            versions.companies.id,
                                                                                            versions.companies.company.id,
                                                                                            versions.companies.company.name,
                                                                                            versions.companies.manufacturer,
                                                                                            versions.platform_version_release_dates.date;
                                                                                     where name ~ *"{escapedSearchInput}"*;
                                                                                     sort name asc;
                                                                                     limit {MaxResults};
                                                                                     offset {searchOffset};
                                                                             """);

        if (cancellationToken.IsCancellationRequested)
        {
            return 0;
        }

        List<PlatformSearchProjection> platformProjections = igdbResults
                                                             .Select(ToPlatformProjection)
                                                             .ToList();
        Dictionary<long, IgdbPlatformVersion> platformVersionsById = await GetPlatformVersions(platformProjections
                                                                                               .SelectMany(projection => projection.PlatformVersionIds)
                                                                                               .Distinct()
                                                                                               .ToArray());

        Dictionary<long, string> manufacturerNamesById = await GetCompanyNames(platformProjections
                                                                               .SelectMany(projection => projection.ManufacturerCompanyIds
                                                                                                                   .Concat(GetManufacturerCompanyIds(GetDetailedPlatformVersions(projection,
                                                                                                                                                                                 platformVersionsById))))
                                                                               .Distinct()
                                                                               .ToArray());
        Dictionary<long, string> manufacturerNamesByPlatformVersionCompanyId =
            await GetPlatformVersionCompanyNames(platformProjections
                                                 .SelectMany(projection => projection.ManufacturerPlatformVersionCompanyIds
                                                                                     .Concat(GetManufacturerPlatformVersionCompanyIds(GetDetailedPlatformVersions(projection,
                                                                                                                                                                  platformVersionsById))))
                                                 .Distinct()
                                                 .ToArray());

        if (cancellationToken.IsCancellationRequested)
        {
            return 0;
        }

        List<IgdbSearchPlatformResult> newResults = platformProjections
                                                    .Select(projection =>
                                                            {
                                                                IEnumerable<IgdbPlatformVersion> detailedPlatformVersions =
                                                                    GetDetailedPlatformVersions(projection, platformVersionsById);

                                                                long[] manufacturerCompanyIds = projection.ManufacturerCompanyIds
                                                                                                          .Concat(GetManufacturerCompanyIds(detailedPlatformVersions))
                                                                                                          .Distinct()
                                                                                                          .ToArray();
                                                                long[] manufacturerPlatformVersionCompanyIds = projection.ManufacturerPlatformVersionCompanyIds
                                                                                                                         .Concat(GetManufacturerPlatformVersionCompanyIds(detailedPlatformVersions))
                                                                                                                         .Distinct()
                                                                                                                         .ToArray();

                                                                string[] manufacturerNames = projection.ManufacturerNames
                                                                                                       .Concat(GetManufacturerCompanyNames(detailedPlatformVersions))
                                                                                                       .Concat(manufacturerCompanyIds
                                                                                                               .Where(manufacturerNamesById.ContainsKey)
                                                                                                               .Select(manufacturerCompanyId => manufacturerNamesById[manufacturerCompanyId]))
                                                                                                       .Concat(manufacturerPlatformVersionCompanyIds
                                                                                                               .Where(manufacturerNamesByPlatformVersionCompanyId.ContainsKey)
                                                                                                               .Select(manufacturerCompanyId => manufacturerNamesByPlatformVersionCompanyId[manufacturerCompanyId]))
                                                                                                       .Where(name => !IsNumericIdList(name))
                                                                                                       .Distinct(StringComparer.OrdinalIgnoreCase)
                                                                                                       .OrderBy(name => name)
                                                                                                       .ToArray();

                                                                if (manufacturerNames.Length == 0
                                                                    && TryGetKnownPlatformManufacturer(projection.Platform.Name, out string? knownManufacturer))
                                                                {
                                                                    manufacturerNames = [knownManufacturer];
                                                                }

                                                                return new IgdbSearchPlatformResult(projection.Platform, manufacturerNames);
                                                            })
                                                    .OrderByNameRelevance(trimmedSearchInput,
                                                                          result => result.Platform.Name)
                                                    .ToList();

        if (appendResults)
        {
            platformResults.AddRange(newResults);
        }
        else
        {
            platformResults = newResults;
        }

        return newResults.Count;
    }

    private async Task<int> SearchCompanies(string trimmedSearchInput, bool appendResults, CancellationToken cancellationToken)
    {
        string escapedSearchInput = EscapeSearchInput(trimmedSearchInput);

        if (!appendResults)
        {
            IgdbCompany[] igdbResults = await IgdbClientProvider
                                              .GetClient()
                                              .QueryAsync<IgdbCompany>(
                                                                       IGDBClient.Endpoints.Companies,
                                                                       query: $"""
                                                                               fields id, name, developed.id, published.id, logo.url;
                                                                               where name ~ *"{escapedSearchInput}"*;
                                                                               sort name asc;
                                                                               limit {CompanyCandidateLimit};
                                                                               """);

            if (cancellationToken.IsCancellationRequested)
            {
                return 0;
            }

            companyResultCandidates = igdbResults
                                      .Select(ToLocalCompany)
                                      .OrderByNameRelevance(trimmedSearchInput, company => company.Name)
                                      .ToList();
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return 0;
        }

        List<LocalCompany> newResults = companyResultCandidates
                                        .Skip(appendResults ? companyResults.Count : 0)
                                        .Take(MaxResults)
                                        .ToList();

        if (appendResults)
        {
            companyResults.AddRange(newResults);
        }
        else
        {
            companyResults = newResults;
        }

        return newResults.Count;
    }

    private async Task SelectGame(LocalGame game)
    {
        SetSelectedSearchInput(game.Name);
        await OnGameSelected.InvokeAsync(game);
    }

    private async Task SelectPlatform(IgdbSearchPlatformResult result)
    {
        SetSelectedSearchInput(result.Platform.Name);
        await OnPlatformSelected.InvokeAsync(result);
    }

    private async Task SelectCompany(LocalCompany company)
    {
        SetSelectedSearchInput(company.Name);
        await OnCompanySelected.InvokeAsync(company);
    }

    private void SetSelectedSearchInput(string name)
    {
        searchCancellationTokenSource?.Cancel();
        searchInput = name;
        activeSearchText = name;
        searchErrorMessage = null;
        canLoadMore = false;
        hasSearched = false;
        ClearResults();
    }

    private void ClearResults()
    {
        gameResults.Clear();
        platformResults.Clear();
        companyResults.Clear();
        companyResultCandidates.Clear();
    }

    private bool CanLoadMoreResults(int resultCount)
    {
        return SearchFor is IgdbSearchFor.Companies
                   ? companyResults.Count < companyResultCandidates.Count
                   : resultCount == MaxResults;
    }

    private static string EscapeSearchInput(string searchInput)
    {
        return searchInput
               .Replace("\\", "\\\\")
               .Replace("\"", "\\\"");
    }

    private LocalGame ToLocalGame(IgdbGame igdbGame)
    {
        HashSet<int> developerCompanyIDs = [];
        HashSet<int> publisherCompanyIDs = [];

        if (igdbGame.InvolvedCompanies?.Values is not null)
        {
            foreach (IgdbInvolvedCompany? involvedCompany in igdbGame.InvolvedCompanies.Values)
            {
                if (involvedCompany?.Developer is true)
                {
                    AddGameCompanyId(developerCompanyIDs, involvedCompany);
                }

                if (involvedCompany?.Publisher is true)
                {
                    AddGameCompanyId(publisherCompanyIDs, involvedCompany);
                }
            }
        }

        LocalGame localGame = new()
                              {
                                  IgdbId = igdbGame.Id ?? 0,
                                  Name = igdbGame.Name ?? string.Empty,
                                  Summary = igdbGame.Summary,
                                  ReleaseDate = ToDateOnly(igdbGame.FirstReleaseDate?.ToUnixTimeSeconds()),
                                  Cover = ToLocalCover(igdbGame.Cover?.Value),
                              };
        localGame.AddCompaniesByID(GameCompanyRole.Developer, developerCompanyIDs.Order().ToList());
        localGame.AddCompaniesByID(GameCompanyRole.Publisher, publisherCompanyIDs.Order().ToList());

        return localGame;
    }

    private void AddGameCompanyId(
        HashSet<int> companyIds,
        IgdbInvolvedCompany involvedCompany)
    {
        long? companyIgdbId = involvedCompany.Company?.Id
                              ?? involvedCompany.Company?.Value?.Id;
        string? companyName = GetCompanyName(involvedCompany.Company);

        LocalCompany? company = ResolveLocalCompany(companyIgdbId, companyName);
        if (company is null)
        {
            return;
        }

        companyIds.Add(company.ID);
    }

    private LocalCompany? ResolveLocalCompany(long? igdbId, string? companyName)
    {
        if (igdbId.HasValue)
        {
            LocalCompany? byIgdbId = LocalCompanies.FirstOrDefault(company => company.IgdbId == igdbId.Value);
            if (byIgdbId is not null)
            {
                return byIgdbId;
            }
        }

        if (string.IsNullOrWhiteSpace(companyName))
        {
            return null;
        }

        return LocalCompanies.FirstOrDefault(company =>
                                                 string.Equals(company.Name,
                                                               companyName,
                                                               StringComparison.OrdinalIgnoreCase));
    }

    private static Image? ToLocalCover(IgdbCover? igdbCover)
    {
        if (igdbCover?.Url is null)
        {
            return null;
        }

        return new Image
               {
                   PendingImageUrl = ToBigCoverUrl(igdbCover.Url)
               };
    }

    private LocalCompany ToLocalCompany(IgdbCompany igdbCompany)
    {
        return new LocalCompany
               {
                   IgdbId = igdbCompany.Id,
                   Name = igdbCompany.Name ?? string.Empty,
                   PendingImageUrl = ToPendingImageUrl(igdbCompany.Logo?.Value),
                   LastSyncedAt = DateTimeOffset.UtcNow
               };
    }

    private static PlatformSearchProjection ToPlatformProjection(IgdbPlatform igdbPlatform)
    {
        List<IgdbPlatformVersion> versions = igdbPlatform.Versions?.Values?.ToList() ?? [];

        LocalPlatform platform = new(igdbPlatform.Name)
                                 {
                                     IgdbId = igdbPlatform.Id,

                                     Abbreviation = igdbPlatform.Abbreviation,
                                     ReleaseDate = GetReleaseDate(versions),
                                     
                                     PendingImageUrl = ToAbsoluteUrl(igdbPlatform.PlatformLogo?.Value?.Url),
                                 };
        return new PlatformSearchProjection(platform,
                                            GetManufacturerCompanyIds(versions),
                                            GetManufacturerCompanyNames(versions),
                                            GetManufacturerPlatformVersionCompanyIds(versions),
                                            GetPlatformVersionIds(versions));
    }

    private async Task<Dictionary<long, IgdbPlatformVersion>> GetPlatformVersions(long[] platformVersionIds)
    {
        if (platformVersionIds.Length == 0)
        {
            return [];
        }

        string platformVersionIdsFilter = string.Join(",", platformVersionIds);

        IgdbPlatformVersion[] platformVersions = await IgdbClientProvider
                                                       .GetClient()
                                                       .QueryAsync<IgdbPlatformVersion>(IGDBClient.Endpoints.PlatformVersions,
                                                                                        query: $"""
                                                                                                fields id,
                                                                                                       main_manufacturer.id,
                                                                                                       main_manufacturer.company.id,
                                                                                                       main_manufacturer.company.name,
                                                                                                       companies.id,
                                                                                                       companies.company.id,
                                                                                                       companies.company.name,
                                                                                                       companies.manufacturer;
                                                                                                where id = ({platformVersionIdsFilter});
                                                                                                limit {platformVersionIds.Length};
                                                                                                """);

        return platformVersions
               .Where(platformVersion => platformVersion.Id.HasValue)
               .ToDictionary(platformVersion => platformVersion.Id!.Value);
    }

    private async Task<Dictionary<long, string>> GetCompanyNames(long[] companyIds)
    {
        if (companyIds.Length == 0)
        {
            return [];
        }

        string companyIdsFilter = string.Join(",", companyIds);

        IgdbCompany[] companies = await IgdbClientProvider
                                        .GetClient()
                                        .QueryAsync<IgdbCompany>(IGDBClient.Endpoints.Companies,
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

    private async Task<Dictionary<long, string>> GetPlatformVersionCompanyNames(long[] platformVersionCompanyIds)
    {
        if (platformVersionCompanyIds.Length == 0)
        {
            return [];
        }

        string platformVersionCompanyIdsFilter = string.Join(",", platformVersionCompanyIds);

        IgdbPlatformVersionCompany[] companies = await IgdbClientProvider
                                                       .GetClient()
                                                       .QueryAsync<IgdbPlatformVersionCompany>(IGDBClient.Endpoints.PlatformVersionCompanies,
                                                                                               query: $"""
                                                                                                       fields id, company.id, company.name, manufacturer;
                                                                                                       where id = ({platformVersionCompanyIdsFilter});
                                                                                                       limit {platformVersionCompanyIds.Length};
                                                                                                       """);

        return companies
               .Where(company => company.Id.HasValue
                                 && !string.IsNullOrWhiteSpace(GetCompanyName(company.Company)))
               .ToDictionary(company => company.Id!.Value,
                             company => GetCompanyName(company.Company)!);
    }

    public static long[] GetManufacturerCompanyIds(IEnumerable<IgdbPlatformVersion> versions)
    {
        return versions
               .SelectMany(version =>
                           {
                               long? mainManufacturerVersionCompanyId = version.MainManufacturer?.Id
                                                                        ?? version.MainManufacturer?.Value?.Id;

                               IEnumerable<long> mainManufacturerCompanyIds =
                                   GetCompanyIds(version.MainManufacturer?.Value?.Company);

                               IEnumerable<long> manufacturerCompanyIds =
                                   version.Companies?.Values?
                                          .Where(company => company.Manufacturer is true)
                                          .SelectMany(company => GetCompanyIds(company.Company))
                                   ?? [];

                               IEnumerable<long> matchedMainManufacturerCompanyIds =
                                   version.Companies?.Values?
                                          .Where(company => company.Id.HasValue
                                                            && company.Id == mainManufacturerVersionCompanyId)
                                          .SelectMany(company => GetCompanyIds(company.Company))
                                   ?? [];

                               return mainManufacturerCompanyIds
                                      .Concat(manufacturerCompanyIds)
                                      .Concat(matchedMainManufacturerCompanyIds);
                           })
               .Distinct()
               .ToArray();
    }

    private static long[] GetManufacturerPlatformVersionCompanyIds(IEnumerable<IgdbPlatformVersion> versions)
    {
        return versions
               .SelectMany(version =>
                           {
                               IEnumerable<long?> mainManufacturerIds =
                                   [version.MainManufacturer?.Id ?? version.MainManufacturer?.Value?.Id];

                               IEnumerable<long?> manufacturerIds =
                                   version.Companies?.Values?
                                          .Where(company => company.Manufacturer is true)
                                          .Select(company => company.Id)
                                   ?? [];

                               return mainManufacturerIds.Concat(manufacturerIds);
                           })
               .Where(companyId => companyId.HasValue)
               .Select(companyId => companyId!.Value)
               .Distinct()
               .ToArray();
    }

    private static long[] GetPlatformVersionIds(IEnumerable<IgdbPlatformVersion> versions)
    {
        return versions
               .Where(version => version.Id.HasValue)
               .Select(version => version.Id!.Value)
               .Distinct()
               .ToArray();
    }

    private static IEnumerable<IgdbPlatformVersion> GetDetailedPlatformVersions(
        PlatformSearchProjection projection,
        IReadOnlyDictionary<long, IgdbPlatformVersion> platformVersionsById)
    {
        return projection.PlatformVersionIds
                         .Where(platformVersionsById.ContainsKey)
                         .Select(platformVersionId => platformVersionsById[platformVersionId]);
    }

    private static string[] GetManufacturerCompanyNames(IEnumerable<IgdbPlatformVersion> versions)
    {
        return versions
               .SelectMany(version =>
                           {
                               long? mainManufacturerVersionCompanyId = version.MainManufacturer?.Id
                                                                        ?? version.MainManufacturer?.Value?.Id;

                               IEnumerable<string?> mainManufacturerCompanyNames =
                                   [GetCompanyName(version.MainManufacturer?.Value?.Company)];

                               IEnumerable<string?> manufacturerCompanyNames =
                                   version.Companies?.Values?
                                          .Where(company => company.Manufacturer is true)
                                          .Select(company => GetCompanyName(company.Company))
                                   ?? [];

                               IEnumerable<string?> matchedMainManufacturerCompanyNames =
                                   version.Companies?.Values?
                                          .Where(company => company.Id.HasValue
                                                            && company.Id == mainManufacturerVersionCompanyId)
                                          .Select(company => GetCompanyName(company.Company))
                                   ?? [];

                               return mainManufacturerCompanyNames
                                      .Concat(manufacturerCompanyNames)
                                      .Concat(matchedMainManufacturerCompanyNames);
                           })
               .Where(name => !string.IsNullOrWhiteSpace(name))
               .Select(name => name!)
               .Distinct(StringComparer.OrdinalIgnoreCase)
               .ToArray();
    }

    private static long[] GetCompanyIds(IdentityOrValue<IgdbCompany>? company)
    {
        long? companyId = company?.Id ?? company?.Value?.Id;

        if (companyId.HasValue)
        {
            return [companyId.Value];
        }

        return [];
    }

    private static string? GetCompanyName(IdentityOrValue<IgdbCompany>? company)
    {
        string? companyName = company?.Value?.Name;

        return IsNumericIdList(companyName)
                   ? null
                   : companyName;
    }

    private static bool IsNumericIdList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .All(part => long.TryParse(part, out _));
    }

    private static DateOnly? GetReleaseDate(IEnumerable<IgdbPlatformVersion> versions)
    {
        DateTimeOffset? firstReleaseDate = versions
                                           .SelectMany(version => version.PlatformVersionReleaseDates?.Values ?? [])
                                           .Where(release => release.Date.HasValue)
                                           .Select(release => (DateTimeOffset?)release.Date!.Value)
                                           .OrderBy(date => date)
                                           .FirstOrDefault();

        return firstReleaseDate.HasValue
                   ? DateOnly.FromDateTime(firstReleaseDate.Value.UtcDateTime)
                   : null;
    }

    private static DateOnly? ToDateOnly(long? unixTime)
    {
        return unixTime.HasValue
                   ? DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(unixTime.Value).UtcDateTime)
                   : null;
    }

    private static string? ToPendingImageUrl(IgdbCompanyLogo? igdbCompanyLogo)
    {
        return ToBigCoverUrl(igdbCompanyLogo?.Url);
    }

    private static string? ToAbsoluteUrl(string? url)
    {
        if (url is null)
        {
            return null;
        }

        return url.StartsWith("//")
                   ? $"https:{url}"
                   : url;
    }

    private static string? ToBigCoverUrl(string? url)
    {
        string? absoluteUrl = ToAbsoluteUrl(url);

        return absoluteUrl?.Replace("/t_thumb/", "/t_cover_big/", StringComparison.OrdinalIgnoreCase);
    }

    private string GetGameSummary(LocalGame game)
    {
        string releaseSummary = game.ReleaseDate.HasValue
                                    ? game.ReleaseDate.Value.ToString("yyyy-MM-dd")
                                    : "Unknown release date";

        string companySummary = string.Join(", ",
                                            game.GetAllCompanyIDs()
                                                .Distinct()
                                                .Select(GetLocalCompanyName)
                                                .Where(value => !string.IsNullOrWhiteSpace(value))
                                                .Distinct(StringComparer.OrdinalIgnoreCase));

        return string.IsNullOrWhiteSpace(companySummary)
                   ? releaseSummary
                   : $"{companySummary} · {releaseSummary}";
    }

    private static string GetPlatformSummary(IgdbSearchPlatformResult result)
    {
        string manufacturerSummary = result.ManufacturerNames.Length == 0
                                         ? "Unknown manufacturer"
                                         : string.Join(", ", result.ManufacturerNames);

        string releaseSummary = result.Platform.ReleaseDate.HasValue
                                    ? result.Platform.ReleaseDate.Value.ToString("yyyy-MM-dd")
                                    : "Unknown release date";

        return $"{manufacturerSummary} · {releaseSummary}";
    }

    private static bool TryGetKnownPlatformManufacturer(string platformName, out string manufacturer)
    {
        (string Prefix, string Manufacturer)[] knownManufacturers =
        [
            ("PlayStation", "Sony"),
            ("Xbox", "Microsoft"),
            ("Nintendo", "Nintendo"),
            ("Steam", "Valve"),
            ("Windows", "Microsoft"),
            ("Mac", "Apple"),
            ("iOS", "Apple"),
            ("Android", "Google")
        ];

        foreach ((string prefix, string knownManufacturer) in knownManufacturers)
        {
            if (platformName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                manufacturer = knownManufacturer;
                return true;
            }
        }

        manufacturer = string.Empty;
        return false;
    }

    private string GetCompanyRoleSummary(LocalCompany company)
    {
        if (!company.IgdbId.HasValue)
        {
            return "Local company";
        }

        LocalCompany? localCompany = LocalCompanies.FirstOrDefault(candidate => candidate.IgdbId == company.IgdbId);
        string[] linkedRoles = localCompany is null
                                   ? []
                                   : GetLinkedRoles(localCompany.ID);

        if (linkedRoles.Length > 0)
        {
            return string.Join(" · ", linkedRoles);
        }

        return "IGDB company";
    }

    private string? GetLocalCompanyName(int companyId)
    {
        return LocalCompanies.FirstOrDefault(company => company.ID == companyId)?.Name;
    }

    private string[] GetLinkedRoles(int companyId)
    {
        return LocalGames
               .SelectMany(game => GetRolesForGameCompany(game, companyId))
               .Distinct()
               .Order()
               .ToArray();
    }

    private static IEnumerable<string> GetRolesForGameCompany(LocalGame game, int companyId)
    {
        List<int> devs = game.GetDeveloperIDs();
        List<int> pubs = game.GetPublisherIDs();
        
        if (devs.Contains(companyId))
        {
            yield return "Developer";
        }

        if (pubs.Contains(companyId))
        {
            yield return "Publisher";
        }
    }

    private static bool IsAuthenticationFailure(Exception exception)
    {
        return exception.Message.Contains("id.twitch.tv/oauth2/token", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record PlatformSearchProjection(
        LocalPlatform Platform,
        long[] ManufacturerCompanyIds,
        string[] ManufacturerNames,
        long[] ManufacturerPlatformVersionCompanyIds,
        long[] PlatformVersionIds);
}

public enum IgdbSearchFor
{
    Games,
    Platforms,
    Companies
}

public sealed record IgdbSearchPlatformResult(LocalPlatform Platform, string[] ManufacturerNames);

internal static class IgdbSearchResultOrderingExtensions
{
    public static IEnumerable<T> OrderByNameRelevance<T>(
        this IEnumerable<T> results,
        string searchInput,
        Func<T, string> getName)
    {
        string normalizedSearchInput = searchInput.Trim();

        return results
               .Select((item, index) => new
                                        {
                                            Item = item,
                                            Index = index,
                                            Name = getName(item)
                                        })
               .OrderBy(result => GetNameRelevanceRank(result.Name, normalizedSearchInput))
               .ThenBy(result => result.Name, StringComparer.OrdinalIgnoreCase)
               .ThenBy(result => result.Index)
               .Select(result => result.Item);
    }

    private static int GetNameRelevanceRank(string name, string searchInput)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return 4;
        }

        string normalizedName = name.Trim();

        if (normalizedName.Equals(searchInput, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (normalizedName.StartsWith(searchInput, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return normalizedName.Contains(searchInput, StringComparison.OrdinalIgnoreCase)
                   ? 2
                   : 3;
    }
}
